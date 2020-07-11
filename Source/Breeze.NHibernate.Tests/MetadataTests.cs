using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Breeze.NHibernate.Configuration;
using Breeze.NHibernate.Metadata;
using Breeze.NHibernate.Tests.Models;
using Breeze.NHibernate.Tests.Models.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Breeze.NHibernate.Tests
{
    public class MetadataTests : BaseDatabaseTest
    {
        private static readonly HashSet<string> VersionedEntityProperties = new HashSet<string>
        {
            nameof(VersionedEntity.CreatedDate),
            nameof(VersionedEntity.LastModifiedDate)
        };

        public MetadataTests(Bootstrapper bootstrapper) : base(bootstrapper)
        {
        }

        [Fact]
        public void TestValidators()
        {
            var container = CreateServiceProvider();
            ConfigureValidators(container);
            var metadata = container.GetService<BreezeMetadataBuilder>()
                .WithClientModelAssemblies(GetClientModelAssemblies())
                .Build();

            var type = (EntityType) metadata.StructuralTypes.First(o => o.Type == typeof(CompositeOrder));
            var properties = type.DataProperties.ToDictionary(o => o.NameOnServer);

            var validator = Assert.Single(properties[nameof(CompositeOrder.Status)].Validators);
            Assert.NotNull(validator);
            Assert.Equal("required", validator.Name);

            var validators = properties[nameof(CompositeOrder.Number)].Validators;
            Assert.Equal(2, validators.Count);
            Assert.Single(validators.Where(o => o.Name == "required"));
            Assert.Single(validators.Where(o => o.Name == "integer"));

            validators = properties[nameof(CompositeOrder.Year)].Validators;
            Assert.Equal(2, validators.Count);
            Assert.Single(validators.Where(o => o.Name == "required"));
            Assert.Single(validators.Where(o => o.Name == "int32"));

            validators = properties[nameof(CompositeOrder.TotalPrice)].Validators;
            Assert.Equal(2, validators.Count);
            Assert.Single(validators.Where(o => o.Name == "required"));
            Assert.Single(validators.Where(o => o.Name == "number"));

            type = (EntityType) metadata.StructuralTypes.First(o => o.Type == typeof(CompositeOrderRow));
            properties = type.DataProperties.ToDictionary(o => o.NameOnServer);

            validator = Assert.Single(properties[$"{nameof(CompositeOrderRow.CompositeOrder)}{nameof(CompositeOrder.Status)}"].Validators);
            Assert.NotNull(validator);
            Assert.Equal("required", validator.Name);

            validators = properties[$"{nameof(CompositeOrderRow.CompositeOrder)}{nameof(CompositeOrder.Number)}"].Validators;
            Assert.Equal(2, validators.Count);
            Assert.Single(validators.Where(o => o.Name == "required"));
            Assert.Single(validators.Where(o => o.Name == "integer"));

            type = (EntityType) metadata.StructuralTypes.First(o => o.Type == typeof(Order));
            properties = type.DataProperties.ToDictionary(o => o.NameOnServer);

            validator = Assert.Single(properties[nameof(Order.CreatedDate)].Validators);
            Assert.NotNull(validator);
            Assert.Equal("date", validator.Name);

            validator = Assert.Single(properties[nameof(Order.LastModifiedDate)].Validators);
            Assert.NotNull(validator);
            Assert.Equal("date", validator.Name);

            validators = properties[nameof(Order.Active)].Validators;
            Assert.Equal(2, validators.Count);
            Assert.Single(validators.Where(o => o.Name == "required"));
            Assert.Single(validators.Where(o => o.Name == "bool"));

            type = (EntityType) metadata.StructuralTypes.First(o => o.Type == typeof(OrderProduct));
            properties = type.DataProperties.ToDictionary(o => o.NameOnServer);

            validators = properties[$"{nameof(OrderProduct.Order)}{nameof(Order.Id)}"].Validators;
            Assert.Equal(2, validators.Count);
            Assert.Single(validators.Where(o => o.Name == "required"));
            Assert.Single(validators.Where(o => o.Name == "integer"));

            validators = properties[$"{nameof(OrderProduct.Product)}{nameof(Product.Id)}"].Validators;
            Assert.Equal(2, validators.Count);
            Assert.Single(validators.Where(o => o.Name == "required"));
            Assert.Single(validators.Where(o => o.Name == "integer"));

            type = (EntityType)metadata.StructuralTypes.First(o => o.Type == typeof(ClientOrder));
            properties = type.DataProperties.ToDictionary(o => o.NameOnServer);

            validators = properties[nameof(ClientOrder.Id)].Validators;
            Assert.Equal(2, validators.Count);
            Assert.Single(validators.Where(o => o.Name == "required"));
            Assert.Single(validators.Where(o => o.Name == "integer"));

            validators = properties[nameof(ClientOrder.CreatedDate)].Validators;
            Assert.Equal(2, validators.Count);
            Assert.Single(validators.Where(o => o.Name == "required"));
            Assert.Single(validators.Where(o => o.Name == "date"));

            validator = Assert.Single(properties[$"{nameof(ClientOrder.MasterOrder)}{nameof(Order.Id)}"].Validators);
            Assert.NotNull(validator);
            Assert.Equal("integer", validator.Name);

            validator = Assert.Single(properties[$"{nameof(ClientOrder.MasterCompositeOrder)}{nameof(CompositeOrder.Number)}"].Validators);
            Assert.NotNull(validator);
            Assert.Equal("integer", validator.Name);

            type = (EntityType) metadata.StructuralTypes.First(o => o.Type == typeof(ClientOrderRow));
            properties = type.DataProperties.ToDictionary(o => o.NameOnServer);

            validators = properties[$"{nameof(ClientOrderRow.ClientOrder)}{nameof(ClientOrder.Id)}"].Validators;
            Assert.Equal(2, validators.Count);
            Assert.Single(validators.Where(o => o.Name == "required"));
            Assert.Single(validators.Where(o => o.Name == "integer"));

            validator = Assert.Single(properties[$"{nameof(ClientOrderRow.Product)}{nameof(Product.Id)}"].Validators);
            Assert.NotNull(validator);
            Assert.Equal("integer", validator.Name);
        }

        [Fact]
        public void TestClientModel()
        {
            var container = CreateServiceProvider();
            ConfigureValidators(container);
            var metadata = container.GetService<BreezeMetadataBuilder>()
                .WithClientModelAssemblies(GetClientModelAssemblies())
                .Build();

            var type = (EntityType) metadata.StructuralTypes.First(o => o.Type == typeof(ClientOrder));
            Assert.Null(type.BaseTypeName);
            Assert.Equal(AutoGeneratedKeyType.KeyGenerator, type.AutoGeneratedKeyType);
            var properties = type.DataProperties.ToDictionary(o => o.NameOnServer);
            Assert.Equal(9, properties.Count);

            Assert.False(properties[nameof(ClientOrder.Id)].IsNullable);
            Assert.False(properties[nameof(ClientOrder.CreatedDate)].IsNullable);
            Assert.True(properties[nameof(ClientOrder.Customer)].IsNullable);

            Assert.True(properties[nameof(ClientOrder.Id)].IsPartOfKey);

            Assert.Equal(DataType.Int64, properties[$"{nameof(ClientOrder.MasterClientOrder)}{nameof(ClientOrder.Id)}"].DataType);
            Assert.Equal(DataType.Int64, properties[$"{nameof(ClientOrder.MasterOrder)}{nameof(Order.Id)}"].DataType);
            Assert.Equal(DataType.Int32, properties[$"{nameof(ClientOrder.MasterCompositeOrder)}{nameof(CompositeOrder.Year)}"].DataType);
            Assert.Equal(DataType.Int64, properties[$"{nameof(ClientOrder.MasterCompositeOrder)}{nameof(CompositeOrder.Number)}"].DataType);
            Assert.Equal(DataType.String, properties[$"{nameof(ClientOrder.MasterCompositeOrder)}{nameof(CompositeOrder.Status)}"].DataType);
            Assert.Equal(DataType.DateTime, properties[nameof(ClientOrder.CreatedDate)].DataType);
            Assert.Equal(DataType.String, properties[nameof(ClientOrder.Customer)].DataType);
            Assert.Null(properties[nameof(ClientOrder.Address)].DataType);
            Assert.Equal("Address:#Breeze.NHibernate.Tests.Models", properties[nameof(ClientOrder.Address)].ComplexTypeName);

            Assert.Equal(6, type.NavigationProperties.Count);
            var navigationProperty = type.NavigationProperties.First(o => o.NameOnServer == nameof(ClientOrder.MasterClientOrder));
            Assert.Equal("AN_ClientOrder_ClientOrder_MasterClientOrderId", navigationProperty.AssociationName);
            Assert.Equal("ClientOrder:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] {"MasterClientOrderId"}, navigationProperty.ForeignKeyNamesOnServer);

            navigationProperty = type.NavigationProperties.First(o => o.NameOnServer == nameof(ClientOrder.MasterOrder));
            Assert.Equal("AN_ClientOrder_Order_MasterOrderId", navigationProperty.AssociationName);
            Assert.Equal("Order:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] {"MasterOrderId"}, navigationProperty.ForeignKeyNamesOnServer);

            navigationProperty = type.NavigationProperties.First(o => o.NameOnServer == nameof(ClientOrder.MasterCompositeOrder));
            Assert.Equal("AN_ClientOrder_CompositeOrder_MasterCompositeOrderYear_MasterCompositeOrderNumber_MasterCompositeOrderStatus", navigationProperty.AssociationName);
            Assert.Equal("CompositeOrder:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] {"MasterCompositeOrderYear", "MasterCompositeOrderNumber", "MasterCompositeOrderStatus"}, navigationProperty.ForeignKeyNamesOnServer);

            navigationProperty = type.NavigationProperties.First(o => o.NameOnServer == nameof(ClientOrder.Orders));
            Assert.Equal("AN_ClientOrder_Order_", navigationProperty.AssociationName);
            Assert.Equal("Order:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.False(navigationProperty.IsScalar);
            Assert.Equal(new string[0], navigationProperty.InvForeignKeyNamesOnServer);

            navigationProperty = type.NavigationProperties.First(o => o.NameOnServer == nameof(ClientOrder.CompositeOrders));
            Assert.Equal("AN_ClientOrder_CompositeOrder_", navigationProperty.AssociationName);
            Assert.Equal("CompositeOrder:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.False(navigationProperty.IsScalar);
            Assert.Equal(new string[0], navigationProperty.InvForeignKeyNamesOnServer);

            navigationProperty = type.NavigationProperties.First(o => o.NameOnServer == nameof(ClientOrder.ClientOrderRows));
            Assert.Equal("AN_ClientOrder_ClientOrderRow_ClientOrderId", navigationProperty.AssociationName);
            Assert.Equal("ClientOrderRow:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.False(navigationProperty.IsScalar);
            Assert.Equal(new[] {"ClientOrderId"}, navigationProperty.InvForeignKeyNamesOnServer);


            type = (EntityType)metadata.StructuralTypes.First(o => o.Type == typeof(ClientOrderRow));
            Assert.Null(type.BaseTypeName);
            Assert.Equal(AutoGeneratedKeyType.KeyGenerator, type.AutoGeneratedKeyType);
            properties = type.DataProperties.ToDictionary(o => o.NameOnServer);

            Assert.False(properties[nameof(ClientOrderRow.Id)].IsNullable);
            Assert.False(properties[nameof(ClientOrderRow.Price)].IsNullable);
            Assert.False(properties[$"{nameof(ClientOrderRow.ClientOrder)}{nameof(ClientOrder.Id)}"].IsNullable);

            Assert.True(properties[nameof(ClientOrder.Id)].IsPartOfKey);

            Assert.Equal(DataType.Int64, properties[$"{nameof(ClientOrderRow.Product)}{nameof(Product.Id)}"].DataType);
            Assert.Equal(DataType.Int64, properties[$"{nameof(ClientOrderRow.ClientOrder)}{nameof(ClientOrder.Id)}"].DataType);

            Assert.Equal(2, type.NavigationProperties.Count);

            navigationProperty = type.NavigationProperties.First(o => o.NameOnServer == nameof(ClientOrderRow.ClientOrder));
            Assert.Equal("AN_ClientOrder_ClientOrderRow_ClientOrderId", navigationProperty.AssociationName);
            Assert.Equal("ClientOrder:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] {"ClientOrderId"}, navigationProperty.ForeignKeyNamesOnServer);

            navigationProperty = type.NavigationProperties.First(o => o.NameOnServer == nameof(ClientOrderRow.Product));
            Assert.Equal("AN_ClientOrderRow_Product_ProductId", navigationProperty.AssociationName);
            Assert.Equal("Product:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] {"ProductId"}, navigationProperty.ForeignKeyNamesOnServer);
        }

        [Fact]
        public void TestInheritance()
        {
            var metadata = GetMetadata(typeof(Animal), typeof(Dog), typeof(Cat));
            var type = (EntityType) metadata.StructuralTypes.First(o => o.Type == typeof(Animal));

            Assert.Null(type.BaseTypeName);
            Assert.Equal(AutoGeneratedKeyType.Identity, type.AutoGeneratedKeyType);

            var properties = type.DataProperties.ToDictionary(o => o.NameOnServer);
            Assert.Equal(7, properties.Count);
            Assert.False(properties[nameof(Animal.Id)].IsNullable);
            Assert.False(properties[nameof(Animal.Name)].IsNullable);
            Assert.False(properties[nameof(Animal.Version)].IsNullable);
            Assert.False(properties[nameof(Animal.BodyWeight)].IsNullable);
            Assert.True(properties[nameof(Animal.CreatedDate)].IsNullable);
            Assert.True(properties[nameof(Animal.LastModifiedDate)].IsNullable);
            Assert.True(properties[$"{nameof(Animal.Parent)}{nameof(Animal.Id)}"].IsNullable);

            Assert.True(properties[nameof(Animal.Id)].IsPartOfKey);
            Assert.False(properties[nameof(Animal.Version)].IsPartOfKey);
            Assert.False(properties[nameof(Animal.Name)].IsPartOfKey);

            Assert.Null(properties[nameof(Animal.Id)].ConcurrencyMode);
            Assert.Equal(ConcurrencyMode.Fixed, properties[nameof(Animal.Version)].ConcurrencyMode);

            Assert.Equal(2, type.NavigationProperties.Count);
            var navigationProperty = type.NavigationProperties.First(o => o.NameOnServer == nameof(Animal.Children));
            Assert.Equal("AN_Animal_Animal_ParentId", navigationProperty.AssociationName);
            Assert.Equal("Animal:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.False(navigationProperty.IsScalar);
            Assert.Equal(new[] {"ParentId"}, navigationProperty.InvForeignKeyNamesOnServer);

            navigationProperty = type.NavigationProperties.First(o => o.NameOnServer == nameof(Animal.Parent));
            Assert.Equal("AN_Animal_Animal_ParentId", navigationProperty.AssociationName);
            Assert.Equal("Animal:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] {"ParentId"}, navigationProperty.ForeignKeyNamesOnServer);

            type = (EntityType) metadata.StructuralTypes.First(o => o.Type == typeof(Dog));
            Assert.Equal("Animal:#Breeze.NHibernate.Tests.Models", type.BaseTypeName);
            Assert.Equal(AutoGeneratedKeyType.Identity, type.AutoGeneratedKeyType);
            properties = type.DataProperties.ToDictionary(o => o.NameOnServer);
            Assert.Equal(3, properties.Count);

            Assert.True(properties[nameof(Dog.Breed)].IsNullable);
            Assert.False(properties[nameof(Dog.Pregnant)].IsNullable);
            Assert.True(properties[nameof(Dog.BirthDate)].IsNullable);

            type = (EntityType)metadata.StructuralTypes.First(o => o.Type == typeof(Cat));
            Assert.Equal("Animal:#Breeze.NHibernate.Tests.Models", type.BaseTypeName);
            Assert.Equal(AutoGeneratedKeyType.Identity, type.AutoGeneratedKeyType);
            properties = type.DataProperties.ToDictionary(o => o.NameOnServer);
            Assert.Equal(3, properties.Count);

            Assert.True(properties[nameof(Cat.Breed)].IsNullable);
            Assert.False(properties[nameof(Cat.Pregnant)].IsNullable);
            Assert.True(properties[nameof(Cat.BirthDate)].IsNullable);
        }

        [Fact]
        public void TestPerson()
        {
            var metadata = GetMetadata(typeof(Person), typeof(IdentityCard), typeof(Passport));
            var type = Assert.Single(metadata.StructuralTypes.OfType<EntityType>().Where(o => o.Type == typeof(Person)));
            Assert.NotNull(type);
            Assert.Null(type.BaseTypeName);
            Assert.Equal(AutoGeneratedKeyType.Identity, type.AutoGeneratedKeyType);

            var properties = type.DataProperties.ToDictionary(o => o.NameOnServer);
            Assert.Equal(8, properties.Count);
            Assert.False(properties[nameof(Person.Id)].IsNullable);
            Assert.False(properties[nameof(Person.Version)].IsNullable);
            Assert.True(properties[nameof(Person.Name)].IsNullable);
            Assert.True(properties[nameof(Person.Surname)].IsNullable);
            Assert.True(properties[nameof(Person.FullName)].IsNullable);
            Assert.True(properties[nameof(Person.CreatedDate)].IsNullable);
            Assert.True(properties[nameof(Person.LastModifiedDate)].IsNullable);
            Assert.True(properties[$"{nameof(Person.Passport)}{nameof(Passport.Id)}"].IsNullable);

            Assert.True(properties[nameof(Person.Id)].IsPartOfKey);
            Assert.False(properties[nameof(Person.Version)].IsPartOfKey);
            Assert.False(properties[nameof(Person.Name)].IsPartOfKey);
            Assert.False(properties[nameof(Person.FullName)].IsPartOfKey);

            Assert.Null(properties[nameof(Person.Id)].ConcurrencyMode);
            Assert.Equal(ConcurrencyMode.Fixed, properties[nameof(Person.Version)].ConcurrencyMode);

            Assert.Equal(2, type.NavigationProperties.Count);
            var navigationProperty = Assert.Single(type.NavigationProperties.Where(o => o.NameOnServer == nameof(Person.IdentityCard)));
            Assert.NotNull(navigationProperty);
            Assert.Equal("AN_IdentityCard_Person_Id", navigationProperty.AssociationName);
            Assert.Equal("IdentityCard:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Null(navigationProperty.ForeignKeyNamesOnServer);
            Assert.Equal(new[] {"Id"}, navigationProperty.InvForeignKeyNamesOnServer);

            navigationProperty = Assert.Single(type.NavigationProperties.Where(o => o.NameOnServer == nameof(Person.Passport)));
            Assert.NotNull(navigationProperty);
            Assert.Equal("AN_Passport_Person_PassportId", navigationProperty.AssociationName);
            Assert.Equal("Passport:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] {"PassportId"}, navigationProperty.ForeignKeyNamesOnServer);
            Assert.Null(navigationProperty.InvForeignKeyNamesOnServer);
        }

        [Fact]
        public void TestPassport()
        {
            var metadata = GetMetadata(typeof(Person), typeof(IdentityCard), typeof(Passport));
            var type = Assert.Single(metadata.StructuralTypes.OfType<EntityType>().Where(o => o.Type == typeof(Passport)));
            Assert.NotNull(type);
            Assert.Null(type.BaseTypeName);
            Assert.Equal(AutoGeneratedKeyType.Identity, type.AutoGeneratedKeyType);

            var properties = type.DataProperties.ToDictionary(o => o.NameOnServer);
            Assert.Equal(4, properties.Count);
            Assert.False(properties[nameof(Passport.Id)].IsNullable);
            Assert.False(properties[nameof(Passport.Version)].IsNullable);
            Assert.True(properties[nameof(Passport.CreatedDate)].IsNullable);
            Assert.True(properties[nameof(Passport.LastModifiedDate)].IsNullable);

            Assert.True(properties[nameof(Passport.Id)].IsPartOfKey);
            Assert.False(properties[nameof(Passport.Version)].IsPartOfKey);

            Assert.Null(properties[nameof(Passport.Id)].ConcurrencyMode);
            Assert.Equal(ConcurrencyMode.Fixed, properties[nameof(Passport.Version)].ConcurrencyMode);

            var navigationProperty = Assert.Single(type.NavigationProperties);
            Assert.NotNull(navigationProperty);
            Assert.Equal(nameof(Passport.Owner), navigationProperty.NameOnServer);
            Assert.Equal("AN_Passport_Person_PassportId", navigationProperty.AssociationName);
            Assert.Equal("Person:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Null(navigationProperty.ForeignKeyNamesOnServer);
            Assert.Equal(new[] {"PassportId"}, navigationProperty.InvForeignKeyNamesOnServer);
        }

        [Fact]
        public void TestIdentityCard()
        {
            var metadata = GetMetadata(typeof(Person), typeof(IdentityCard), typeof(Passport));
            var type = Assert.Single(metadata.StructuralTypes.OfType<EntityType>().Where(o => o.Type == typeof(IdentityCard)));
            Assert.NotNull(type);
            Assert.Null(type.BaseTypeName);
            Assert.Equal(AutoGeneratedKeyType.None, type.AutoGeneratedKeyType);

            var properties = type.DataProperties.ToDictionary(o => o.NameOnServer);
            Assert.Equal(5, properties.Count);
            Assert.False(properties[nameof(IdentityCard.Id)].IsNullable);
            Assert.False(properties[nameof(IdentityCard.Version)].IsNullable);
            Assert.True(properties[nameof(IdentityCard.CreatedDate)].IsNullable);
            Assert.True(properties[nameof(IdentityCard.LastModifiedDate)].IsNullable);
            Assert.True(properties[nameof(IdentityCard.Code)].IsNullable);

            Assert.True(properties[nameof(IdentityCard.Id)].IsPartOfKey);
            Assert.False(properties[nameof(IdentityCard.Version)].IsPartOfKey);

            Assert.Null(properties[nameof(IdentityCard.Id)].ConcurrencyMode);
            Assert.Equal(ConcurrencyMode.Fixed, properties[nameof(IdentityCard.Version)].ConcurrencyMode);

            var navigationProperty = Assert.Single(type.NavigationProperties);
            Assert.NotNull(navigationProperty);
            Assert.Equal(nameof(IdentityCard.Owner), navigationProperty.NameOnServer);
            Assert.Equal("AN_IdentityCard_Person_Id", navigationProperty.AssociationName);
            Assert.Equal("Person:#Breeze.NHibernate.Tests.Models", navigationProperty.EntityTypeName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] {"Id"}, navigationProperty.ForeignKeyNamesOnServer);
            Assert.Null(navigationProperty.InvForeignKeyNamesOnServer);
        }

        [Fact]
        public void TestCompositeOrder()
        {
            var metadata = GetCompositeMetadata();
            var compositeOrderType = (EntityType) metadata.StructuralTypes.First(o => o.Type == typeof(CompositeOrder));
            var properties = compositeOrderType.DataProperties.ToDictionary(o => o.NameOnServer);

            Assert.False(properties[nameof(CompositeOrder.Status)].IsNullable);
            Assert.False(properties[nameof(CompositeOrder.Number)].IsNullable);
            Assert.False(properties[nameof(CompositeOrder.Year)].IsNullable);

            Assert.True(properties[nameof(CompositeOrder.Status)].IsPartOfKey);
            Assert.True(properties[nameof(CompositeOrder.Number)].IsPartOfKey);
            Assert.True(properties[nameof(CompositeOrder.Year)].IsPartOfKey);

            Assert.Equal(DataType.String, properties[nameof(CompositeOrder.Status)].DataType);
            Assert.Equal(DataType.Int64, properties[nameof(CompositeOrder.Number)].DataType);
            Assert.Equal(DataType.Int32, properties[nameof(CompositeOrder.Year)].DataType);

            Assert.Null(compositeOrderType.BaseTypeName);
            Assert.Equal(AutoGeneratedKeyType.None, compositeOrderType.AutoGeneratedKeyType);

            Assert.Equal(2, compositeOrderType.NavigationProperties.Count);

            var navigationProperty = Assert.Single(compositeOrderType.NavigationProperties.Where(o => o.NameOnServer == nameof(CompositeOrder.CompositeOrderRows)));
            Assert.NotNull(navigationProperty);
            Assert.Equal("AN_CompositeOrder_CompositeOrderRow_CompositeOrderYear_CompositeOrderNumber_CompositeOrderStatus", navigationProperty.AssociationName);
            Assert.False(navigationProperty.IsScalar);
            Assert.Equal(new[] {"CompositeOrderYear", "CompositeOrderNumber", "CompositeOrderStatus"}, navigationProperty.InvForeignKeyNamesOnServer);

            navigationProperty = Assert.Single(compositeOrderType.NavigationProperties.Where(o => o.NameOnServer == nameof(CompositeOrder.CompositeOrderProducts)));
            Assert.NotNull(navigationProperty);
            Assert.Equal("AN_CompositeOrder_CompositeOrderProduct_CompositeOrderYear_CompositeOrderNumber_CompositeOrderStatus", navigationProperty.AssociationName);
            Assert.False(navigationProperty.IsScalar);
            Assert.Equal(new[] {"CompositeOrderYear", "CompositeOrderNumber", "CompositeOrderStatus"}, navigationProperty.InvForeignKeyNamesOnServer);
        }

        [Fact]
        public void TestCompositeOrderRow()
        {
            var metadata = GetCompositeMetadata();
            var compositeOrderType = (EntityType)metadata.StructuralTypes.First(o => o.Type == typeof(CompositeOrderRow));
            var properties = compositeOrderType.DataProperties.ToDictionary(o => o.NameOnServer);

            Assert.False(properties[nameof(CompositeOrderRow.Id)].IsNullable);
            Assert.False(properties[nameof(CompositeOrderRow.Price)].IsNullable);
            Assert.False(properties[nameof(CompositeOrderRow.Quantity)].IsNullable);
            Assert.False(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"].IsNullable);
            Assert.False(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"].IsNullable);
            Assert.False(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}"].IsNullable);
            Assert.False(properties[$"{nameof(Product)}{nameof(Product.Id)}"].IsNullable);

            Assert.False(properties[nameof(CompositeOrderRow.Id)].IsUnmapped);
            Assert.False(properties[nameof(CompositeOrderRow.Price)].IsUnmapped);
            Assert.False(properties[nameof(CompositeOrderRow.Quantity)].IsUnmapped);
            Assert.True(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"].IsUnmapped);
            Assert.True(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"].IsUnmapped);
            Assert.True(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}"].IsUnmapped);
            Assert.True(properties[$"{nameof(Product)}{nameof(Product.Id)}"].IsUnmapped);

            Assert.Equal(DataType.Int64, properties[nameof(CompositeOrderRow.Id)].DataType);
            Assert.Equal(DataType.Decimal, properties[nameof(CompositeOrderRow.Price)].DataType);
            Assert.Equal(DataType.Int32, properties[nameof(CompositeOrderRow.Quantity)].DataType);
            Assert.Equal(DataType.Int32, properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"].DataType);
            Assert.Equal(DataType.Int64, properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"].DataType);
            Assert.Equal(DataType.String, properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}"].DataType);
            Assert.Equal(DataType.Int64, properties[$"{nameof(Product)}{nameof(Product.Id)}"].DataType);

            Assert.True(properties[nameof(CompositeOrderRow.Id)].IsPartOfKey);
            Assert.False(properties[nameof(CompositeOrderRow.Price)].IsPartOfKey);
            Assert.False(properties[nameof(CompositeOrderRow.Quantity)].IsPartOfKey);

            Assert.Null(compositeOrderType.BaseTypeName);
            Assert.Equal(AutoGeneratedKeyType.Identity, compositeOrderType.AutoGeneratedKeyType);
            Assert.Equal(2, compositeOrderType.NavigationProperties.Count);

            var navigationProperty = compositeOrderType.NavigationProperties.First(o => o.NameOnServer == nameof(CompositeOrder));
            Assert.Equal("AN_CompositeOrder_CompositeOrderRow_CompositeOrderYear_CompositeOrderNumber_CompositeOrderStatus", navigationProperty.AssociationName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] {"CompositeOrderYear", "CompositeOrderNumber", "CompositeOrderStatus"}, navigationProperty.ForeignKeyNamesOnServer);

            navigationProperty = compositeOrderType.NavigationProperties.First(o => o.NameOnServer == nameof(Product));
            Assert.Equal("AN_CompositeOrderRow_Product_ProductId", navigationProperty.AssociationName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] {"ProductId"}, navigationProperty.ForeignKeyNamesOnServer);
        }

        [Fact]
        public void TestCompositeOrderProduct()
        {
            var metadata = GetCompositeMetadata();
            var compositeOrderType = (EntityType)metadata.StructuralTypes.First(o => o.Type == typeof(CompositeOrderProduct));
            var properties = compositeOrderType.DataProperties.ToDictionary(o => o.NameOnServer);

            Assert.False(properties[nameof(CompositeOrderProduct.Price)].IsNullable);
            Assert.False(properties[nameof(CompositeOrderProduct.Quantity)].IsNullable);
            Assert.False(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"].IsNullable);
            Assert.False(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"].IsNullable);
            Assert.False(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}"].IsNullable);
            Assert.False(properties[$"{nameof(Product)}{nameof(Product.Id)}"].IsNullable);

            Assert.False(properties[nameof(CompositeOrderProduct.Price)].IsUnmapped);
            Assert.False(properties[nameof(CompositeOrderProduct.Quantity)].IsUnmapped);
            Assert.True(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"].IsUnmapped);
            Assert.True(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"].IsUnmapped);
            Assert.True(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}"].IsUnmapped);
            Assert.True(properties[$"{nameof(Product)}{nameof(Product.Id)}"].IsUnmapped);

            Assert.Equal(DataType.Decimal, properties[nameof(CompositeOrderProduct.Price)].DataType);
            Assert.Equal(DataType.Int32, properties[nameof(CompositeOrderProduct.Quantity)].DataType);
            Assert.Equal(DataType.Int32, properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"].DataType);
            Assert.Equal(DataType.Int64, properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"].DataType);
            Assert.Equal(DataType.String, properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}"].DataType);
            Assert.Equal(DataType.Int64, properties[$"{nameof(Product)}{nameof(Product.Id)}"].DataType);

            Assert.False(properties[nameof(CompositeOrderProduct.Price)].IsPartOfKey);
            Assert.False(properties[nameof(CompositeOrderProduct.Quantity)].IsPartOfKey);
            Assert.True(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"].IsPartOfKey);
            Assert.True(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"].IsPartOfKey);
            Assert.True(properties[$"{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}"].IsPartOfKey);
            Assert.True(properties[$"{nameof(Product)}{nameof(Product.Id)}"].IsPartOfKey);

            Assert.Null(compositeOrderType.BaseTypeName);
            Assert.Equal(AutoGeneratedKeyType.None, compositeOrderType.AutoGeneratedKeyType);
            Assert.Equal(3, compositeOrderType.NavigationProperties.Count);

            var navigationProperty = compositeOrderType.NavigationProperties.First(o => o.NameOnServer == nameof(CompositeOrder));
            Assert.Equal("AN_CompositeOrder_CompositeOrderProduct_CompositeOrderYear_CompositeOrderNumber_CompositeOrderStatus", navigationProperty.AssociationName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] {"CompositeOrderYear", "CompositeOrderNumber", "CompositeOrderStatus"}, navigationProperty.ForeignKeyNamesOnServer);

            navigationProperty = compositeOrderType.NavigationProperties.First(o => o.NameOnServer == nameof(Product));
            Assert.Equal("AN_CompositeOrderProduct_Product_ProductId", navigationProperty.AssociationName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] {"ProductId"}, navigationProperty.ForeignKeyNamesOnServer);

            navigationProperty = Assert.Single(compositeOrderType.NavigationProperties.Where(o => o.NameOnServer == nameof(CompositeOrderProduct.Remarks)));
            Assert.NotNull(navigationProperty);
            Assert.Equal("AN_CompositeOrderProduct_CompositeOrderProductRemark_CompositeOrderProductCompositeOrderYear_CompositeOrderProductCompositeOrderNumber_CompositeOrderProductCompositeOrderStatus_CompositeOrderProductProductId", navigationProperty.AssociationName);
            Assert.False(navigationProperty.IsScalar);
            Assert.Equal(
                new[]
                {
                    "CompositeOrderProductCompositeOrderYear", "CompositeOrderProductCompositeOrderNumber",
                    "CompositeOrderProductCompositeOrderStatus", "CompositeOrderProductProductId"
                }, navigationProperty.InvForeignKeyNamesOnServer);
        }

        [Fact]
        public void TestCompositeOrderProductRemark()
        {
            var metadata = GetCompositeMetadata();
            var compositeOrderType = (EntityType)metadata.StructuralTypes.First(o => o.Type == typeof(CompositeOrderProductRemark));
            var properties = compositeOrderType.DataProperties.ToDictionary(o => o.NameOnServer);

            Assert.False(properties[nameof(CompositeOrderProductRemark.Id)].IsNullable);
            Assert.True(properties[nameof(CompositeOrderProductRemark.Remark)].IsNullable);
            Assert.False(properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"].IsNullable);
            Assert.False(properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"].IsNullable);
            Assert.False(properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}"].IsNullable);
            Assert.False(properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(Product)}{nameof(Product.Id)}"].IsNullable);

            Assert.Equal(500, properties[nameof(CompositeOrderProductRemark.Remark)].MaxLength);

            Assert.False(properties[nameof(CompositeOrderProductRemark.Id)].IsUnmapped);
            Assert.False(properties[nameof(CompositeOrderProductRemark.Remark)].IsUnmapped);
            Assert.True(properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"].IsUnmapped);
            Assert.True(properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"].IsUnmapped);
            Assert.True(properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}"].IsUnmapped);
            Assert.True(properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(Product)}{nameof(Product.Id)}"].IsUnmapped);

            Assert.Equal(DataType.Int64, properties[nameof(CompositeOrderProductRemark.Id)].DataType);
            Assert.Equal(DataType.String, properties[nameof(CompositeOrderProductRemark.Remark)].DataType);
            Assert.Equal(DataType.Int32, properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"].DataType);
            Assert.Equal(DataType.Int64, properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"].DataType);
            Assert.Equal(DataType.String, properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}"].DataType);
            Assert.Equal(DataType.Int64, properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(Product)}{nameof(Product.Id)}"].DataType);

            Assert.True(properties[nameof(CompositeOrderProductRemark.Id)].IsPartOfKey);
            Assert.False(properties[nameof(CompositeOrderProductRemark.Remark)].IsPartOfKey);
            Assert.False(properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"].IsPartOfKey);
            Assert.False(properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"].IsPartOfKey);
            Assert.False(properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}"].IsPartOfKey);
            Assert.False(properties[$"{nameof(CompositeOrderProductRemark.CompositeOrderProduct)}{nameof(Product)}{nameof(Product.Id)}"].IsPartOfKey);

            Assert.Null(compositeOrderType.BaseTypeName);
            Assert.Equal(AutoGeneratedKeyType.Identity, compositeOrderType.AutoGeneratedKeyType);

            var navigationProperty = Assert.Single(compositeOrderType.NavigationProperties);
            Assert.NotNull(navigationProperty);
            Assert.Equal(nameof(CompositeOrderProduct), navigationProperty.NameOnServer);
            Assert.Equal("AN_CompositeOrderProduct_CompositeOrderProductRemark_CompositeOrderProductCompositeOrderYear_CompositeOrderProductCompositeOrderNumber_CompositeOrderProductCompositeOrderStatus_CompositeOrderProductProductId", navigationProperty.AssociationName);
            Assert.True(navigationProperty.IsScalar);
            Assert.Equal(new[] { "CompositeOrderProductCompositeOrderYear", "CompositeOrderProductCompositeOrderNumber", "CompositeOrderProductCompositeOrderStatus", "CompositeOrderProductProductId" }, navigationProperty.ForeignKeyNamesOnServer);
        }

        [Fact]
        public void TestDefaultValue()
        {
            var container = CreateServiceProvider();
            var configurator = container.GetService<IBreezeConfigurator>();
            configurator.ConfigureModel<OrderProduct>()
                .ForMember(o => o.TotalPrice, o => o.DefaultValue(10.5m));

            var metadata = GetMetadata(container, typeof(Order), typeof(OrderProduct));
            var type = (EntityType)metadata.StructuralTypes.First(o => o.Type == typeof(OrderProduct));
            var property = Assert.Single(type.DataProperties.Where(o => o.NameOnServer == $"{nameof(OrderProduct.TotalPrice)}"));
            Assert.NotNull(property);
            Assert.Equal(10.5m, property.DefaultValue);
        }

        [Fact]
        public void TestIgnoreAssociationProperty()
        {
            var container = CreateServiceProvider();
            var configurator = container.GetService<IBreezeConfigurator>();
            configurator.ConfigureModel<OrderProduct>()
                .ForMember(o => o.Order, o => o.Ignore());
            configurator.ConfigureModel<OrderProductFk>()
                .ForMember(o => o.Order, o => o.Ignore());

            var metadata = GetMetadata(container, typeof(Order), typeof(OrderProduct), typeof(OrderProductFk));
            var type = (EntityType)metadata.StructuralTypes.First(o => o.Type == typeof(OrderProduct));
            Assert.Empty(type.NavigationProperties.Where(o => o.NameOnServer == nameof(Order)));
            Assert.Empty(type.DataProperties.Where(o => o.NameOnServer == $"{nameof(Order)}{nameof(Order.Id)}"));

            type = (EntityType)metadata.StructuralTypes.First(o => o.Type == typeof(OrderProductFk));
            Assert.Empty(type.NavigationProperties.Where(o => o.NameOnServer == nameof(Order)));
            Assert.Single(type.DataProperties.Where(o => o.NameOnServer == $"{nameof(Order)}{nameof(Order.Id)}"));
        }

        [Fact]
        public void TestIgnoreAssociationKeepFkProperty()
        {
            var container = CreateServiceProvider();
            var configurator = container.GetService<IBreezeConfigurator>();
            configurator.ConfigureModel<OrderProduct>()
                .ForMember(o => o.Order, o => o.Ignore())
                .ForSyntheticMember<long>("OrderId", o => o.Include());

            var metadata = GetMetadata(container, typeof(Order), typeof(OrderProduct), typeof(OrderProductFk));
            var type = (EntityType)metadata.StructuralTypes.First(o => o.Type == typeof(OrderProduct));
            Assert.Empty(type.NavigationProperties.Where(o => o.NameOnServer == nameof(Order)));
            Assert.Single(type.DataProperties.Where(o => o.NameOnServer == $"{nameof(Order)}{nameof(Order.Id)}"));
        }

        private BreezeMetadata GetCompositeMetadata()
        {
            return GetMetadata(typeof(CompositeOrder), typeof(CompositeOrderRow), typeof(Product), typeof(CompositeOrderProduct), typeof(CompositeOrderProductRemark));
        }

        private BreezeMetadata GetMetadata(params Type[] includeTypes)
        {
            var container = CreateServiceProvider();
            ConfigureValidators(container);
            return GetMetadata(container, includeTypes);
        }

        private BreezeMetadata GetMetadata(ServiceProvider container, params Type[] includeTypes)
        {
            var types = new HashSet<Type>(includeTypes);
            var metadata = container.GetService<BreezeMetadataBuilder>()
                .WithIncludeFilter(t => types.Contains(t))
                .Build();

            Assert.Equal(types.Count, metadata.StructuralTypes.OfType<EntityType>().Count());
            return metadata;
        }

        private static IEnumerable<Assembly> GetClientModelAssemblies()
        {
            yield return typeof(Order).Assembly;
        }

        private static void ConfigureValidators(ServiceProvider container)
        {
            var configurator = container.GetService<IBreezeConfigurator>();
            configurator.ConfigureModelMembers(o => typeof(IClientModel).IsAssignableFrom(o),
                (member, o) =>
                {
                    if (member.GetCustomAttribute<NotNullAttribute>() != null)
                    {
                        o.IsNullable(false);
                    }
                });
            configurator.ConfigureModelMembers(o => typeof(VersionedEntity).IsAssignableFrom(o),
                (member, o) =>
                {
                    if (VersionedEntityProperties.Contains(member.Name))
                    {
                        o.IsNullable(true);
                    }
                });
        }
    }
}
