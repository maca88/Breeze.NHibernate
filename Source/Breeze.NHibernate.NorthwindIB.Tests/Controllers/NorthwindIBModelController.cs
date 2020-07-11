using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Transactions;
using Breeze.NHibernate.AspNetCore.Mvc;
using Breeze.NHibernate.Configuration;
using Breeze.NHibernate.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models.NorthwindIB.NH;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Breeze.NHibernate.NorthwindIB.Tests.Controllers
{
    [Route("breeze/[controller]/[action]")]
    [ServiceFilter(typeof(BreezeQueryFilter))]
    public class NorthwindIBModelController : Controller
    {
        private readonly ISession _session;
        private readonly NorthwindPersistenceManager _persistenceManager;
        private readonly BreezeMetadata _metadata;

        public NorthwindIBModelController(
            ISession session,
            NorthwindPersistenceManager persistenceManager,
            BreezeMetadata metadata)
        {
            _session = session;
            _persistenceManager = persistenceManager;
            _metadata = metadata;
        }

        [HttpGet]
        public string Metadata()
        {
            return _metadata.ToJson();
        }

        [HttpPost]
        public SaveResult SaveChanges([FromBody]SaveBundle saveBundle)
        {
            return SaveChangesCore(saveBundle);
        }

        #region Transactions

        [HttpPost]
        public SaveResult SaveWithTransactionScope([FromBody]SaveBundle saveBundle)
        {
            var txOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TransactionManager.DefaultTimeout,
            };

            using var txScope = new TransactionScope(TransactionScopeOption.Required, txOptions);
            var result = SaveChangesCore(saveBundle);
            txScope.Complete();
            return result;
        }

        [HttpPost]
        public SaveResult SaveWithDbTransaction([FromBody]SaveBundle saveBundle)
        {
            return SaveChangesCore(saveBundle);
        }

        [HttpPost]
        public SaveResult SaveWithNoTransaction([FromBody]SaveBundle saveBundle)
        {
            return _persistenceManager.SaveChanges(saveBundle);
        }

        #endregion

        #region Interceptors

        [HttpPost]
        public SaveResult SaveWithComment([FromBody]SaveBundle saveBundle)
        {
            return SaveChangesCore(saveBundle, configurator => configurator.BeforeApplyChanges(
                context =>
                {
                    var comment = new Comment();
                    var tag = context.SaveOptions.Tag.ToString();
                    comment.Comment1 = (tag == null) ? "Generic comment" : tag.ToString();
                    comment.CreatedOn = DateTime.Now;
                    comment.SeqNum = 1;
                    context.AddAdditionalEntity(comment, EntityState.Added);
                }));
        }

        [HttpPost]
        public SaveResult SaveWithExit([FromBody]SaveBundle saveBundle)
        {
            return new SaveResult {Entities = new List<object>(), KeyMappings = new List<KeyMapping>()};
        }

        [HttpPost]
        public SaveResult SaveAndThrow([FromBody]SaveBundle saveBundle)
        {
            return SaveChangesCore(saveBundle, configurator => configurator.BeforeApplyChanges(
                context => throw new Exception("Deliberately thrown exception")));
        }

        [HttpPost]
        public SaveResult SaveWithEntityErrorsException([FromBody]SaveBundle saveBundle)
        {
            return SaveChangesCore(saveBundle, configurator => configurator.BeforeApplyChanges(
                context =>
                {
                    var saveMap = context.SaveMap;
                    if (saveMap.TryGetValue(typeof(Order), out var orderInfos))
                    {
                        var errors = orderInfos.Select(oi =>
                        {
                            return new EntityError
                            {
                                EntityTypeName = typeof(Order).FullName,
                                ErrorMessage = "Cannot save orders with this save method",
                                ErrorName = "WrongMethod",
                                KeyValues = new object[] {((Order)oi.ClientEntity).OrderID},
                                PropertyName = "OrderID"
                            };
                        });
                        var ex = new EntityErrorsException("test of custom exception message", errors);
                        // if you want to see a different error status code use this.
                        // ex.StatusCode = HttpStatusCode.Conflict; // Conflict = 409 ; default is Forbidden (403).
                        throw ex;
                    }
                }));
        }

        [HttpPost]
        public SaveResult SaveWithAuditFields([FromBody]SaveBundle saveBundle)
        {
            return SaveChangesCore(saveBundle, configurator => configurator.BeforeSaveEntityChanges((entityInfo, context) =>
            {
                var entity = entityInfo.Entity as User;
                if (entity == null)
                {
                    return;
                }

                var userId = 12345;
                if (entityInfo.EntityState == EntityState.Added)
                {
                    entity.CreatedBy = "test";
                    entity.CreatedByUserId = userId;
                    entity.CreatedDate = DateTime.Now;
                    entity.ModifiedBy = "test";
                    entity.ModifiedByUserId = userId;
                    entity.ModifiedDate = DateTime.Now;
                }
                else if (entityInfo.EntityState == EntityState.Modified)
                {
                    entity.ModifiedBy = "test";
                    entity.ModifiedByUserId = userId;
                    entity.ModifiedDate = DateTime.Now;
                }
            }));
        }

        [HttpPost]
        public SaveResult SaveWithFreight([FromBody]SaveBundle saveBundle)
        {
            return SaveChangesCore(saveBundle, configurator => configurator.BeforeSaveEntityChanges(CheckFreight));
        }

        [HttpPost]
        public SaveResult SaveWithFreight2([FromBody]SaveBundle saveBundle)
        {
            return SaveChangesCore(saveBundle, configurator => configurator.BeforeSaveChanges((order, context) =>
            {
                var saveMap = context.SaveMap;
                if (saveMap.TryGetValue(typeof(Order), out var entityInfos))
                {
                    foreach (var entityInfo in entityInfos)
                    {
                        CheckFreight(entityInfo, context);
                    }
                }
            }));
        }

        [HttpPost]
        public SaveResult SaveCheckInitializer([FromBody]SaveBundle saveBundle)
        {
            return SaveChangesCore(saveBundle, configurator => configurator.BeforeApplyChanges(context =>
            {
                var order = new Order {OrderDate = DateTime.Today};
                context.AddAdditionalEntity(order, EntityState.Added);
            }));
        }

        [HttpPost]
        public SaveResult SaveCheckUnmappedProperty([FromBody]SaveBundle saveBundle)
        {
            return SaveChangesCore(saveBundle, configurator => configurator.AfterCreateEntityInfo((entityInfo, saveOptions) =>
            {
                var unmappedValue = (string)entityInfo.UnmappedValuesMap["MyUnmappedProperty"];
                if (unmappedValue != "anything22")
                {
                    throw new Exception("wrong value for unmapped property:  " + unmappedValue);
                }

                return false;
            }));
        }

        [HttpPost]
        public SaveResult SaveCheckUnmappedPropertySerialized([FromBody]SaveBundle saveBundle)
        {
            return SaveChangesCore(saveBundle, configurator => configurator.AfterCreateEntityInfo((entityInfo, saveOptions) =>
            {
                var unmappedValue = (string)entityInfo.UnmappedValuesMap["MyUnmappedProperty"];
                if (unmappedValue != "ANYTHING22")
                {
                    throw new Exception("wrong value for unmapped property:  " + unmappedValue);
                }

                var anotherOne = entityInfo.UnmappedValuesMap["AnotherOne"];
                if (((dynamic)anotherOne).z[5].foo.Value != 4)
                {
                    throw new Exception("wrong value for 'anotherOne.z[5].foo'");
                }

                if (((dynamic)anotherOne).extra.Value != 666)
                {
                    throw new Exception("wrong value for 'anotherOne.extra'");
                }

                var customer = (Customer)entityInfo.ClientEntity;
                if (customer.CompanyName.ToUpper() != customer.CompanyName)
                {
                    throw new Exception("Uppercasing of company name did not occur");
                }

                return false;
            }));
        }

        [HttpPost]
        public SaveResult SaveCheckUnmappedPropertySuppressed([FromBody]SaveBundle saveBundle)
        {
            return SaveChangesCore(saveBundle, configurator => configurator.AfterCreateEntityInfo((entityInfo, saveOptions) =>
            {
                if (entityInfo.UnmappedValuesMap != null)
                {
                    throw new Exception("unmapped properties should have been suppressed");
                }

                return false;
            }));
        }

        #endregion

        #region Standard queries

        [HttpGet]
        public IQueryable<Customer> Customers()
        {
            return _persistenceManager.Customers;
        }

        [HttpGet]
        public IQueryable<Order> Orders()
        {
            return _persistenceManager.Orders;
        }

        [HttpGet]
        public IQueryable<Employee> Employees()
        {
            return _persistenceManager.Employees;
        }

        [HttpGet]
        public IQueryable<OrderDetail> OrderDetails()
        {
            return _persistenceManager.OrderDetails;
        }

        [HttpGet]
        public IQueryable<Product> Products()
        {
            return _persistenceManager.Products;
        }

        [HttpGet]
        public IQueryable<Supplier> Suppliers()
        {
            return _persistenceManager.Suppliers;
        }

        [HttpGet]
        public IQueryable<Region> Regions()
        {
            return _persistenceManager.Regions;
        }

        [HttpGet]
        public IQueryable<Territory> Territories()
        {
            return _persistenceManager.Territories;
        }

        [HttpGet]
        public IQueryable<Category> Categories()
        {
            return _persistenceManager.Categories;
        }

        [HttpGet]
        public IQueryable<Role> Roles()
        {
            return _persistenceManager.Roles;
        }

        [HttpGet]
        public IQueryable<User> Users()
        {
            return _persistenceManager.Users;
        }

        [HttpGet]
        public IQueryable<TimeLimit> TimeLimits()
        {
            return _persistenceManager.TimeLimits;
        }

        [HttpGet]
        public IQueryable<TimeGroup> TimeGroups()
        {
            return _persistenceManager.TimeGroups;
        }

        [HttpGet]
        public IQueryable<Comment> Comments()
        {
            return _persistenceManager.Comments;
        }

        [HttpGet]
        public IQueryable<UnusualDate> UnusualDates()
        {
            return _persistenceManager.UnusualDates;
        }

        #endregion

        #region Named queries

        [HttpGet]
        public IQueryable<Customer> CustomersStartingWith(string companyName)
        {
            if (companyName == "null")
            {
                throw new Exception("nulls should not be passed as 'null'");
            }

            if (string.IsNullOrEmpty(companyName))
            {
                companyName = "";
            }

            return _persistenceManager.Customers.Where(c => c.CompanyName.StartsWith(companyName));
        }

        [HttpGet]
        public object CustomerCountsByCountry()
        {
            return _persistenceManager.Customers.GroupBy(c => c.Country).Select(g => new {g.Key, Count = g.Count()});
        }

        [HttpGet]
        public Customer CustomerWithScalarResult()
        {
            return _persistenceManager.Customers.First();
        }

        [HttpGet]
        public IActionResult CustomersWithHttpError()
        {
            return StatusCode(StatusCodes.Status404NotFound, "Custom error message");
        }

        [HttpGet]
        public IEnumerable<Employee> EnumerableEmployees()
        {
            return _persistenceManager.Employees.ToList();
        }

        [HttpGet]
        public IQueryable<Employee> EmployeesFilteredByCountryAndBirthdate(DateTime birthDate, string country)
        {
            return _persistenceManager.Employees.Where(emp => emp.BirthDate >= birthDate && emp.Country == country);
        }

        [HttpGet]
        public List<Employee> QueryInvolvingMultipleEntities()
        {
            var pm = _persistenceManager;
            var query = (from t1 in pm.Employees
                where (from t2 in pm.Orders select t2.EmployeeID).Distinct().Contains(t1.EmployeeID)
                select t1);
            var result = query.ToList();
            return result;
        }

        [HttpGet]
        public List<Customer> CustomerFirstOrDefault()
        {
            var customer = _persistenceManager.Customers.FirstOrDefault(c => c.CompanyName.StartsWith("blah"));
            return customer == null ? new List<Customer>() : new List<Customer> {customer};
        }

        [HttpGet]
        public int OrdersCountForCustomer(string companyName)
        {
            var customer = _persistenceManager.Customers.Include(o => o.Orders).First(c => c.CompanyName.StartsWith(companyName));
            return customer.Orders.Count;
        }

        [HttpGet]
        // AltCustomers will not be in the resourceName/entityType map;
        public IQueryable<Customer> AltCustomers()
        {
            return _persistenceManager.Customers;
        }

        [HttpGet]
        // public IQueryable<Employee> SearchEmployees([FromUri] int[] employeeIds) {
        // // may need to use FromRoute... as opposed to FromQuery
        public IQueryable<Employee> SearchEmployees([FromQuery] int[] employeeIds)
        {
            var query = _persistenceManager.Employees.AsQueryable();
            if (employeeIds.Length > 0)
            {
                query = query.Where(emp => employeeIds.Contains(emp.EmployeeID));
                var result = query.ToList();
            }

            return query;
        }

        public class CustomerQBE
        {
            public String CompanyName { get; set; }
            public String[] ContactNames { get; set; }
            public String City { get; set; }
        }

        [HttpGet]
        public IQueryable<Customer> SearchCustomers([FromQuery] CustomerQBE qbe)
        {
            // var query = ContextProvider.Context.Customers.Where(c =>
            //    c.CompanyName.StartsWith(qbe.CompanyName));
            var ok = qbe != null && qbe.CompanyName != null & qbe.ContactNames.Length > 0 && qbe.City.Length > 1;
            if (!ok)
            {
                throw new Exception("qbe error");
            }
            // just testing that qbe actually made it in not attempted to write qbe logic here
            // so just return first 3 customers.
            return _persistenceManager.Customers.Take(3);
        }

        [HttpGet]
        public IQueryable<Customer> SearchCustomers2([FromQuery] CustomerQBE[] qbeList)
        {

            if (qbeList.Length < 2)
            {
                throw new Exception("all least two items must be passed in");
            }
            var ok = qbeList.All(qbe => qbe.CompanyName != null & qbe.ContactNames.Length > 0 && qbe.City.Length > 1);
            if (!ok)
            {
                throw new Exception("qbeList error");
            }
            // just testing that qbe actually made it in not attempted to write qbe logic here
            // so just return first 3 customers.
            return _persistenceManager.Customers.Take(3);
        }

        [HttpGet]
        public IQueryable<Customer> CustomersOrderedStartingWith(string companyName)
        {
            var customers = _persistenceManager.Customers.Where(c => c.CompanyName.StartsWith(companyName)).OrderBy(cust => cust.CompanyName);
            var list = customers.ToList();
            return customers;
        }

        [HttpGet]
        public IQueryable<Employee> EmployeesMultipleParams(int employeeID, string city)
        {
            // HACK: 
            if (city == "null")
            {
                city = null;
            }

            return _persistenceManager.Employees.Where(emp => emp.EmployeeID == employeeID || emp.City.Equals(city));
        }

        [HttpGet]
        public IEnumerable<Object> Lookup1Array()
        {
            var regions = _persistenceManager.Regions;
            return new List<object> {new {regions = regions}};
        }

        [HttpGet]
        public object Lookups()
        {
            var regions = _persistenceManager.Regions;
            var territories = _persistenceManager.Territories;
            var categories = _persistenceManager.Categories;

            return new {regions, territories, categories};
        }

        [HttpGet]
        public IEnumerable<object> LookupsEnumerableAnon()
        {
            var regions = _persistenceManager.Regions;
            var territories = _persistenceManager.Territories;
            var categories = _persistenceManager.Categories;

            return new List<object> {new {regions = regions, territories = territories, categories = categories}};
        }

        [HttpGet]
        public IQueryable<object> CompanyNames()
        {
            return _persistenceManager.Customers.Select(c => c.CompanyName);
        }

        [HttpGet]
        public IQueryable<object> CompanyNamesAndIds()
        {
            return _persistenceManager.Customers.Select(c => new {c.CompanyName, c.CustomerID});
        }

        public class CustomerDTO
        {
            public CustomerDTO()
            {
            }

            public CustomerDTO(String companyName, Guid customerID)
            {
                CompanyName = companyName;
                CustomerID = customerID;
            }

            public Guid CustomerID { get; set; }
            public String CompanyName { get; set; }
            public AnotherType AnotherItem { get; set; }
        }

        public class AnotherType
        {

        }

        [HttpGet]
        public IQueryable<CustomerDTO> CompanyNamesAndIdsAsDTO()
        {
            return _persistenceManager.Customers.Select(c => new CustomerDTO {CompanyName = c.CompanyName, CustomerID = c.CustomerID});
        }

        [HttpGet]
        public IQueryable<object> CustomersWithBigOrders()
        {
            return _persistenceManager.Customers.Where(c => c.Orders.Any(o => o.Freight > 100)).Select(c => new {Customer = c, BigOrders = c.Orders.Where(o => o.Freight > 100)});
        }

        [HttpGet]
        public IQueryable<object> CompanyInfoAndOrders()
        {
            // Need to handle this specially for NH, to prevent $top being applied to Orders
            var q = _persistenceManager.Customers.Select(c => new {c.CompanyName, c.CustomerID, c.Orders});

            return q.ToList().AsQueryable();
        }

        [HttpGet]
        public object CustomersAndProducts()
        {
            return new
            {
                Customers = _persistenceManager.Customers.ToList(), Products = _persistenceManager.Products.ToList()
            };
        }

        [HttpGet]
        public IQueryable<object> TypeEnvelopes()
        {
            return GetType().Assembly.GetTypes()
                .Select(t => new { t.Assembly.FullName, t.Name, t.Namespace })
                .AsQueryable();
        }


        [HttpGet]
        public IQueryable<Customer> CustomersAndOrders()
        {
            return _persistenceManager.Customers.Include(o => o.Orders);
        }

        [HttpGet]
        public IQueryable<Order> OrdersAndCustomers()
        {
            return _persistenceManager.Orders.Include("Customer");
        }


        [HttpGet]
        public IQueryable<Customer> CustomersStartingWithA()
        {
            return _persistenceManager.Customers.Where(c => c.CompanyName.StartsWith("A"));
        }

        [HttpGet]
        public IActionResult CustomersAsHRM()
        {
            return Ok(_persistenceManager.Customers.Cast<Customer>());
        }

        [HttpGet]
        public IQueryable<OrderDetail> OrderDetailsMultiple(int multiple, string expands)
        {
            var query = _persistenceManager.OrderDetails.OfType<OrderDetail>();
            if (!string.IsNullOrWhiteSpace(expands))
            {
                var segs = expands.Split(',').ToList();
                segs.ForEach(s =>
                {
                    query = ((dynamic)query).Include(s);
                });
            }

            var orig = query.ToList() as IList<OrderDetail>;
            var list = new List<OrderDetail>(orig.Count * multiple);
            for (var i = 0; i < multiple; i++)
            {
                for (var j = 0; j < orig.Count; j++)
                {
                    var od = orig[j];
                    var newProductID = i * j + 1;
                    var clone = new OrderDetail();
                    clone.Order = od.Order;
                    clone.OrderID = od.OrderID;
                    clone.RowVersion = od.RowVersion;
                    clone.UnitPrice = od.UnitPrice;
                    clone.Quantity = (short)multiple;
                    clone.Discount = i;
                    clone.ProductID = newProductID;

                    if (od.Product != null)
                    {
                        var p = new Product();
                        var op = od.Product;
                        p.ProductID = newProductID;
                        p.Category = op.Category;
                        p.CategoryID = op.CategoryID;
                        p.Discontinued = op.Discontinued;
                        p.DiscontinuedDate = op.DiscontinuedDate;
                        p.ProductName = op.ProductName;
                        p.QuantityPerUnit = op.QuantityPerUnit;
                        p.ReorderLevel = op.ReorderLevel;
                        p.RowVersion = op.RowVersion;
                        p.Supplier = op.Supplier;
                        p.SupplierID = op.SupplierID;
                        p.UnitPrice = op.UnitPrice;
                        p.UnitsInStock = op.UnitsInStock;
                        p.UnitsOnOrder = op.UnitsOnOrder;
                        clone.Product = p;
                    }

                    list.Add(clone);
                }
            }

            return list.AsQueryable();
        }

        #endregion

        private static void CheckFreight(EntityInfo entityInfo, SaveChangesContext context)
        {
            var order = entityInfo.Entity as Order;
            order.Freight = order.Freight + 1;
        }

        private SaveResult SaveChangesCore(SaveBundle saveBundle, Action<SaveChangesOptionsConfigurator> configureAction = null)
        {
            using var tx = _session.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            try
            {
                var result = _persistenceManager.SaveChanges(saveBundle, configureAction);
                tx.Commit();
                return result;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

    }
}
