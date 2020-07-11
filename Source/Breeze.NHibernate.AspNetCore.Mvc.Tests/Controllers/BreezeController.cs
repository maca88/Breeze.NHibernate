using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Breeze.NHibernate.Metadata;
using Breeze.NHibernate.Tests.Models;
using Microsoft.AspNetCore.Mvc;
using NHibernate;

namespace Breeze.NHibernate.AspNetCore.Mvc.Tests.Controllers
{
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
        public SaveResult SaveChanges(SaveBundle saveBundle)
        {
            return _persistenceManager.SaveChanges(saveBundle);
        }

        [HttpPost]
        public Task<SaveResult> AsyncSaveChanges(SaveBundle saveBundle)
        {
            return _persistenceManager.SaveChangesAsync(saveBundle);
        }

        [HttpGet]
        public string Metadata()
        {
            return _metadata.ToJson();
        }

        [HttpGet]
        public IQueryable<Order> EntityErrorsException()
        {
            throw new EntityErrorsException(new List<EntityError>
            {
                new EntityError
                {
                    PropertyName =  "Test",
                    EntityTypeName = "Order",
                    ErrorMessage = "Test error",
                    ErrorName = "error",
                    KeyValues = null
                }
            });
        }

        [HttpGet]
        public IQueryable<Order> Orders()
        {
            return _session.Query<Order>();
        }

        [HttpGet]
        public IQueryable<ProductDto> LazyLoad()
        {
            return _session.Query<OrderProduct>().Select(o => new ProductDto()
            {
                OrderProduct = o
            });
        }

        public class ProductDto
        {
            public OrderProduct OrderProduct { get; set; }

            public string OrderName => OrderProduct.Order.Name;
        }
    }
}
