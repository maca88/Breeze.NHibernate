Breeze.NHiberante
=================

Breeze.NHiberante is a [Breeze][] server side implementation for NHibernate, which is a complete re-write of [breeze.server.net][] except for querying, which was modified to support synthetic foreign key properties.

## Key features
- Supports synthetic foreign key properties
- Extend is implemented with [NHibernate.Extensions][] `Include` method when supported
- Provides async methods
- Customizable metadata with a fluent API
- Customizable entity serialization with a fluent API
- Supports multiple session factories
- Supports non entity types in metadata
- Supports batch fetching entities upon saving
- Supports custom synthetic properties
- Supports validating [DDD Aggregate][] roots

## Differences from [breeze.server.net][]
- `BreezeQueryFilter` does not close the NHibernate session after the query is executed
- `PersistenceManager` does not contain breeze metadata
- `PersistenceManager` does not manage transactions, its only purpose is to persist entity changes
- `ISession.Refresh` is not called by default
- Protected property setters of mapped entities are writable by default upon deserialization
- Only mapped properties are serialized by default
- Collections in `EntityInfo.Entity` are populated
- Bi-directional associations are updated when applying changes (e.g. in one-to-many relation the child is removed/added from the parent collection)
- The logic that determines the save order of entities takes into consideration also the association cascade style (e.g. `save-update`, `all`, ...)
- Lazy load exceptions that occurs while serializing a query result are thrown instead of swallowed
- When saving an entity with an old version an exception is thrown

## Getting started
- Install package [Breeze.NHibernate.AspNetCore.Mvc](https://www.nuget.org/packages/Breeze.NHibernate.AspNetCore.Mvc/) via NuGet to your ASP.NET Core Mvc project

- Add the following code into the `Startup.ConfigureServices` method:

```C#
// Configure controllers to use Newtonsoft Json.NET serializer
services.AddControllers().AddNewtonsoftJson();

// Configure Breeze.NHibernate by using the default global exception filter for handling EntityErrorsException
services.AddBreezeNHibernate(options => options.WithGlobalExceptionFilter());
```
- Create a controller that will be used by the breeze client:

```C#
[ApiController]
[Route("[controller]/[action]")]
[ServiceFilter(typeof(BreezeQueryFilter))]
public class BreezeController : ControllerBase
{
    private readonly ISession _session;
    private readonly PersistenceManager _persistenceManager;
    private readonly BreezeMetadata _metadata;

    public BreezeController(
        ISession session,
        PersistenceManager persistenceManager,
        BreezeMetadata metadata)
    {
        _session = session;
        _persistenceManager = persistenceManager;
        _metadata = metadata;
    }

    [HttpPost]
    public Task<SaveResult> SaveChanges(SaveBundle saveBundle)
    {
        return _persistenceManager.SaveChangesAsync(saveBundle);
    }

    [HttpGet]
    public string Metadata()
    {
        return _metadata.ToJson();
    }
    
    [HttpGet]
    public IQueryable<Order> Orders()
    {
        return _session.Query<Order>();
    }
}
```
- Register `ISession` and `ISessionFactory` with the dependency injection. Registration example:
```C#
services.AddSingleton(s => CreateNHibernateConfiguration());
services.AddSingleton(s => s.GetService<NHConfiguration>().BuildSessionFactory());
services.AddScoped(s => s.GetService<ISessionFactory>().OpenSession());
services.AddScoped(s => s.GetService<ISession>().BeginTransaction());
```
- Create a transaction per request, by creating a middleware ([Example](https://github.com/maca88/Breeze.NHibernate/blob/master/Source/Breeze.NHibernate.AspNetCore.Mvc.Tests/PerRequestTransactionMiddleware.cs)) and register it in `Startup.Configure` method:
```C#
app.UsePerRequestTransaction();
```
or in case it is desired that the transaction is created only for saving changes, handle the transaction inside `SaveChanges` method:
```C#
public async Task<SaveResult> SaveChanges(SaveBundle saveBundle)
{
    using var tx = _session.BeginTransaction(IsolationLevel.ReadCommitted);
    try
    {
        var result = await _persistenceManager.SaveChangesAsync(saveBundle);
        tx.Commit();
        return result;
    }
    catch
    {
        tx.Rollback();
        throw;
    }
}
```

A complete example can be found [here](https://github.com/maca88/Breeze.NHibernate/tree/master/Source/Breeze.NHibernate.AspNetCore.Mvc.Tests).

## Synthetic foreign key properties

NHibernate supports having associations without defining foreign key properties, example:
```C#
public class OrderRow
{
    public virtual int Id  { get; set; }

    public virtual Order Order { get; set; }
}
```
but because breeze client requires them (`OrderId` in the above example), we have to define them so that they will be added to the metadata. In order to avoid defining them in code, we take advantage of [unmapped properties](http://breeze.github.io/doc-js/entity-serialization.html) feature, where all foreign key properties that are not defined in code will be added to the metadata as unmapped properties. By default the foreign key property names are generated by concatenating the association property name with the primary key property name of the associated entity. In order to change the default naming convention, register your implementation of `ISyntheticPropertyNameConvention` interface before calling `AddBreezeNHibernate` method. For example:
```C#
public class UnderscoreSyntheticPropertyNameConvention : ISyntheticPropertyNameConvention
{
    public string GetName(string associationPropertyName, string associationPkPropertyName)
    {
        return $"{associationPropertyName}_{associationPkPropertyName}";
    }
}

// Register it before calling services.AddBreezeNHibernate
services.AddSingleton<ISyntheticPropertyNameConvention, UnderscoreSyntheticPropertyNameConvention>();
```
will generate `Order_Id` instead.

Synthetic foreign key properties can normally be used in breeze queries, for example:
```js
EntityQuery.from('OrderRow').where('orderId', '==', 1)
```
will be translated to the following Linq query:
```C#
Query<OrderRow>().Where(o => o.Order.Id == 1)
```

## Customizable metadata

By default breeze metadata is populated with all mapped entities and for each mapped entity only the its mapped properties are added. To control which entities should be added to the metadata, use `WithMetadataConfigurator` when calling `AddBreezeNHibernate` method:
```C#
services.AddBreezeNHibernate(options => options
    .WithMetadataConfigurator(c => c.WithIncludeFilter(o => o.Namespace == "MyNamespace")));
```
To control which properties will be added to the metadata, use `WithBreezeConfigurator` when calling `AddBreezeNHibernate` method:
```C#
services.AddBreezeNHibernate(options => options
    .WithBreezeConfigurator(c => c.ConfigureModel<User>()
        .ForMember(o => o.NotMapped, o => o.Include()) // Adds NotMapped property to the metadata as an unmapped property
        .ForMember(o => o.PasswordHash, o => o.Ignore()))); // Removes PasswordHash property from the metadata
```
Note that the `Include` and `Ignore` methods from the above example also modify the property serialization behavior.

## Customizable serialization

By default only mapped properties are serialized in order to avoid exposing data that the client may not use. To change the default behavior, use `WithBreezeConfigurator` when calling `AddBreezeNHibernate` method:
```C#
services.AddBreezeNHibernate(options => options
    .WithBreezeConfigurator(c => c.ConfigureModel<User>()
        .ForMember(o => o.NotMapped, o => o.Include()) // Includes NotMapped property when serializing an User instance
        .ForMember(o => o.PasswordHash, o => o.Ignore()))); // Omits PasswordHash property when serializing an User instance
```
Note that the `Include` and `Ignore` methods from the above example also add/remove the property from the metadata.
In case you want to include a property getter that may initialize a proxy when accessed, you need to use `ShouldSerialize` method and pass a predicate that checks whether the proxy is initialized:
```C#
public class OrderRow
{
    public virtual int Id  { get; set; }

    public virtual Order Order { get; set; }
    
    public virtual string OrderName => Order.Name;
}
// Include OrderName when serializing OrderRow
services.AddBreezeNHibernate(options => options
    .WithBreezeConfigurator(c => c.ConfigureModel<OrderRow>()
        .ForMember(o => o.OrderName, o => o.Include().ShouldSerialize(or => NHibernateUtil.IsInitialized(or.Order)))
```
otherwise an exception will be thrown upon serialization, when `Order` property won't be initialized.

## Multiple session factories

By default we provide support for a single session factory, which will satisfy most users. In order to support multiple session factories the following two interfaces needs to be implemented:
- `ISessionProvider`
- `INHibernateClassMetadataProvider`

and registered before calling `AddBreezeNHibernate` method. There are many ways how those two interfaces can be implemented, [here](https://github.com/maca88/Breeze.NHibernate/blob/master/Source/Breeze.NHibernate.AspNetCore.Mvc.Tests/MultipleDatabaseStartup.cs) you can find one example.

## Non entity types (client models)

In addition to mapped entities we support adding custom server models to the metadata so that they can be also used by the client. In order to enable this feature we have to first specify where our types are located, by using `WithClientModelAssemblies` method:
```C#
services.AddBreezeNHibernate(options => options
    .WithMetadataConfigurator(c => c.WithClientModelAssemblies(new[] {typeof(ClientOrder).Assembly})));
```
and then create a class that implements `IClientModel`:
```C#
public class ClientOrder : IClientModel
{
    public long Id { get; set; }

    public Order Order { get; set; }

    public List<ClientOrderRow> Rows { get; set; }
}
```
the `Id` property is mandatory as breeze requires at least one key property in order to work. The custom defined model can contains associations with mapped entities or other classes implementing `IClientModel` and synthetic foreign key properties will also be generated for them.

## Custom synthetic properties

Apart from synthetic foreign key properties, which are automatically created, we provide an api to add custom synthetic properties to types, by using `AddSyntheticMember`:
```
services.AddBreezeNHibernate(options => options
    .WithBreezeConfigurator(c => c.ConfigureModel<OrderRow>()
        .AddSyntheticMember("DiscountedPrice", or => or.Price * 0.8m)));
```
The above example will add `DiscountedPrice` property to the metadata of `OrderRow` type and the result of the provided delegate will be included when serializing an instance of `OrderRow`.

## DDD Aggregate roots

When saving a [DDD aggregate][], a cluster of domain objects that can be treated as a single unit (e.g. an order with its rows), we want to assure that all entities that are sent from the client belong to the same aggregate root. For example, when creating an order and its rows, a malicious client may send additional entities that are not related to the created order:
```js
var order = manager.createEntity('Order', {
  name:'Name',
  address:'Address'
  rows: [
    {
      productId: 1,
      price: 20
    }
  ]
});
// A malicious creates an order row that does not belong to the created order
manager.createEntity('OrderRow', {orderId: 12, product: 1, price: 0});
// Save all changes
manager.saveChanges(null, new SaveOptions({ resourceName: 'SaveOrder' }))
```
in order to avoid writing a validation logic that prevents such things to happen, we provide an overload for `SaveChanges` method, which validates entities sent from the client:
```C#
persistenceManager.SaveChanges<Order>(saveBundle);
```
The default validator validates the following:
- Only one aggregate root must exist in the saving graph
- For every parent and child must exist a bi-directional association
- The cascade style of the inverse association property of a child must allow the performing operation

A different validator can be provided per save operation:
```C#
persistenceManager.SaveChanges<Order>(saveBundle, c => c.ModelSaveValidator(new CustomModelSaveValidator()));
```
or changing the default one by registering a custom implementation of `IModelSaveValidatorProvider` and register it before calling `AddBreezeNHibernate` method.



[DDD aggregate]: https://martinfowler.com/bliki/DDD_Aggregate.html
[NHibernate.Extensions]: https://github.com/maca88/NHibernate.Extensions
[Breeze]: http://www.getbreezenow.com/breezejs
[breeze.server.net]: http://github.com/breeze/breeze.server.net