using System.Linq;
using Breeze.NHibernate.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Sample_WebApi2.Models;

namespace Breeze.NHibernate.NorthwindIB.Tests.Controllers
{
    [Route("breeze/[controller]/[action]")]
    [ServiceFilter(typeof(BreezeQueryFilter))]
    public class NonEFModelController : Controller
    {
        private readonly NonEFModelContext _context;

        public NonEFModelController()
        {
            _context = new NonEFModelContext();
        }

        [HttpGet]
        public IQueryable<Person> Persons()
        {
            return _context.Persons;
        }

        [HttpGet]
        public IQueryable<Meal> Meals()
        {
            return _context.Meals;
        }
    }
}
