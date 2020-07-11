using System.Linq;
using System.Threading.Tasks;
using Breeze.NHibernate.Metadata;
using Breeze.NHibernate.Tests.Models;
using Microsoft.AspNetCore.Mvc;

namespace Breeze.NHibernate.AspNetCore.Mvc.Tests.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    [ServiceFilter(typeof(BreezeQueryFilter))]
    public class MultipleDatabaseController : ControllerBase
    {
        private readonly BarSession _barSession;
        private readonly FooSession _fooSession;
        private readonly PersistenceManager _persistenceManager;
        private readonly BreezeMetadata _metadata;

        public MultipleDatabaseController(
            PersistenceManager persistenceManager,
            BreezeMetadata metadata,
            BarSession barSession,
            FooSession fooSession)
        {
            _persistenceManager = persistenceManager;
            _metadata = metadata;
            _barSession = barSession;
            _fooSession = fooSession;
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
        public IQueryable<Order> Orders()
        {
            return _barSession.Session.Query<Order>();
        }

        [HttpGet]
        public IQueryable<CompositeOrder> CompositeOrders()
        {
            return _fooSession.Session.Query<CompositeOrder>();
        }
    }
}
