using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Breeze.NHibernate.Validation;
using Models.NorthwindIB.NH;
using NHibernate;

using BreezeEntityState = Breeze.NHibernate.EntityState;

namespace Breeze.NHibernate.NorthwindIB.Tests
{
    public class NorthwindPersistenceManager : NorthwindNHPersistenceManager
    {
        public NorthwindPersistenceManager(
            EntityUpdater entityUpdater,
            ISaveWorkStateFactory saveWorkStateFactory,
            IModelSaveValidatorProvider modelSaveValidatorProvider,
            BreezeEntityValidator breezeEntityValidator,
            ISession session)
            : base(entityUpdater, saveWorkStateFactory, modelSaveValidatorProvider, breezeEntityValidator, session)
        {
        }

        protected override bool AfterCreateEntityInfo(EntityInfo entityInfo, SaveOptions saveOptions)
        {
            // prohibit any additions of entities of type 'Region'
            if (entityInfo.EntityType == typeof(Region) && entityInfo.EntityState == BreezeEntityState.Added)
            {
                var region = entityInfo.ClientEntity as Region;
                if (region.RegionDescription.ToLowerInvariant().StartsWith("error"))
                {
                    return false;
                }
            }

            return true;
        }

        protected override void BeforeApplyChanges(SaveChangesContext context)
        {
            var tag = context.SaveOptions.Tag?.ToString();
            var saveMap = context.SaveMap;
            if (tag == "CommentOrderShipAddress.Before")
            {
                var orderInfos = saveMap[typeof(Order)];
                byte seq = 1;
                foreach (var info in orderInfos)
                {
                    var order = (Order)info.ClientEntity;
                    AddComment(order.ShipAddress, seq++);
                }
            }
            else if (tag == "UpdateProduceShipAddress.Before")
            {
                var orderInfos = saveMap[typeof(Order)];
                var order = (Order)orderInfos[0].ClientEntity;
                UpdateProduceDescription(order.ShipAddress);
            }
            else if (tag == "LookupEmployeeInSeparateContext.Before")
            {
                LookupEmployeeInSeparateContext(false);
            }
            else if (tag == "LookupEmployeeInSeparateContext.SameConnection.Before")
            {
                LookupEmployeeInSeparateContext(true);
            }
            else if (tag == "ValidationError.Before")
            {
                foreach (var type in saveMap.Keys)
                {
                    var list = saveMap[type];
                    foreach (var entityInfo in list)
                    {
                        var entity = entityInfo.ClientEntity;
                        var entityError = new EntityError()
                        {
                            EntityTypeName = type.Name,
                            ErrorMessage = "Error message for " + type.Name,
                            ErrorName = "Server-Side Validation",
                        };
                        if (entity is Order)
                        {
                            var order = (Order)entity;
                            entityError.KeyValues = new object[] { order.OrderID };
                            entityError.PropertyName = "OrderDate";
                        }
                    }
                }
            }
            else if (tag == "increaseProductPrice")
            {
                foreach (var type in saveMap.Keys)
                {
                    if (type == typeof(Category))
                    {
                        foreach (var entityInfo in saveMap[type])
                        {
                            if (entityInfo.EntityState == BreezeEntityState.Modified)
                            {
                                Category category = (entityInfo.ClientEntity as Category);
                                var products = Products.Where(p => p.CategoryID == category.CategoryID);
                                foreach (var product in products)
                                {
                                    context.AddAdditionalEntity(product, BreezeEntityState.Modified);
                                    var incr = (Convert.ToInt64(product.UnitPrice) % 2) == 0 ? 1 : -1;
                                    product.UnitPrice += incr;
                                }
                            }
                        }
                    }
                }
            }
            else if (tag == "deleteProductOnServer.Before")
            {
                var prodinfo = saveMap[typeof(Product)].First();
                if (prodinfo.EntityState == BreezeEntityState.Added)
                {
                    // because Deleted throws error when trying delete non-existent row from database
                    prodinfo.EntityState = BreezeEntityState.Detached;
                }
                else
                {
                    prodinfo.EntityState = BreezeEntityState.Deleted;
                }
            }
            else if (tag == "deleteSupplierOnServer.Before")
            {
                var product = (Product)saveMap[typeof(Product)].First().ClientEntity;
                var infos = GetEntityInfos(saveMap, typeof(Supplier));
                var supinfo = infos?.FirstOrDefault();
                if (supinfo != null)
                {
                    supinfo.EntityState = BreezeEntityState.Deleted;
                }
                else
                {
                    // create new EntityInfo for entity that we want to delete that was not in the save bundle
                    var supplier = Session.Get<Supplier>(product.SupplierID);
                    context.AddAdditionalEntity(supplier, BreezeEntityState.Deleted);
                }
            }
        }

        protected override void BeforeSaveEntityChanges(EntityInfo entityInfo, SaveChangesContext context)
        {
            var tag = context.SaveOptions.Tag?.ToString();
            if (tag == "addProdOnServer")
            {
                Supplier supplier = entityInfo.Entity as Supplier;
                Product product = new Product
                {
                    ProductName = "Product added on server"
                };

                supplier.Products.Add(product);
            }
        }

        protected override void AfterFlushChanges(SaveChangesContext context, List<KeyMapping> keyMappings)
        {
            var saveMap = context.SaveMap;
            var tag = context.SaveOptions.Tag?.ToString();
            if (tag == "CommentKeyMappings.After")
            {
                foreach (var km in keyMappings)
                {
                    var realint = Convert.ToInt32(km.RealValue);
                    byte seq = (byte)(realint % 512);
                    AddComment(km.EntityTypeName + ':' + km.RealValue, seq);
                }
            }
            else if (tag == "UpdateProduceKeyMapping.After")
            {
                if (!keyMappings.Any()) throw new Exception("UpdateProduce.After: No key mappings available");
                var km = keyMappings[0];
                UpdateProduceDescription(km.EntityTypeName + ':' + km.RealValue);

            }
            else if (tag == "LookupEmployeeInSeparateContext.After")
            {
                LookupEmployeeInSeparateContext(false);
            }
            else if (tag == "LookupEmployeeInSeparateContext.SameConnection.After")
            {
                LookupEmployeeInSeparateContext(true);
            }
            else if (tag == "deleteProductOnServer")
            {
                var t = typeof(Product);
                var prodinfo = saveMap[t].First();
                prodinfo.EntityState = BreezeEntityState.Deleted;
            }
            else if (tag != null && tag.StartsWith("deleteProductOnServer:"))
            {
                // create new EntityInfo for entity that we want to delete that was not in the save bundle
                var id = tag.Substring(tag.IndexOf(':') + 1);
                var product = new Product {ProductID = int.Parse(id)};
                context.AddAdditionalEntity(product, BreezeEntityState.Deleted);
            }
            else if (tag == "deleteSupplierAndProductOnServer")
            {
                // mark deleted entities that are in the save bundle
                var t = typeof(Product);
                var infos = GetEntityInfos(saveMap, typeof(Product));
                var prodinfo = infos.FirstOrDefault();
                if (prodinfo != null) prodinfo.EntityState = BreezeEntityState.Deleted;
                infos = GetEntityInfos(saveMap, typeof(Supplier));
                var supinfo = infos.FirstOrDefault();
                if (supinfo != null) supinfo.EntityState = BreezeEntityState.Deleted;
            }
        }

        protected override bool HandleSaveException(Exception exception, SaveWorkState saveWorkState)
        {
            // breeze.server.net does not check the version property, throw the expected exception in case the entity was
            // already updated when the second save is triggered
            if (exception is EntityErrorsException entityErrors && entityErrors.Message == "Cannot update an old version")
            {
                throw new HibernateException("Row was updated or deleted by another transaction");
            }

            return base.HandleSaveException(exception, saveWorkState);
        }

        private int AddComment(string comment, byte seqnum)
        {
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var text = $"insert into Comment (CreatedOn, Comment1, SeqNum) values ('{time}', '{comment}', {seqnum})";
            var cmd = Session.CreateSQLQuery(text);
            var result = cmd.ExecuteUpdate();

            return result;
        }

        // Test performing a raw db update to ProduceTPH using the ProduceTPH connection.  Requires DTC.
        private int UpdateProduceDescription(string comment)
        {
            using var conn = new SqlConnection("data source=.;initial catalog=ProduceTPH;integrated security=True;multipleactiveresultsets=True;application name=EntityFramework");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = String.Format("update ItemOfProduce set Description='{0}' where id='{1}'",
                comment, "13F1C9F5-3189-45FA-BA6E-13314FAFAA92");
            var result = cmd.ExecuteNonQuery();
            conn.Close();
            return result;
        }

        // Use another Context to simulate lookup.  Returns Margaret Peacock if employeeId is not specified.
        private Employee LookupEmployeeInSeparateContext(bool existingConnection, int employeeId = 4)
        {
            var context2 = this; // TODO new NorthwindNHPersistenceManager(this.Session.SessionFactory);
            var query = context2.Employees.Where(e => e.EmployeeID == employeeId);
            var employee = query.FirstOrDefault();
            return employee;
        }

        private List<EntityInfo> GetEntityInfos(Dictionary<Type, List<EntityInfo>> saveMap, Type t)
        {
            List<EntityInfo> entityInfos;
            if (!saveMap.TryGetValue(t, out entityInfos))
            {
                return null;
            }

            return entityInfos;
        }
    }
}
