using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Breeze.NHibernate.Serialization;
using Breeze.NHibernate.Validation;
using Models.NorthwindIB.NH;
using NHibernate;

namespace Breeze.NHibernate.NorthwindIB.Tests
{
    public class NorthwindNHPersistenceManager : PersistenceManager
    {
        protected readonly ISession Session;
        protected readonly BreezeEntityValidator BreezeEntityValidator;

        public NorthwindNHPersistenceManager(
            EntityUpdater entityUpdater,
            ISaveWorkStateFactory saveWorkStateFactory,
            IModelSaveValidatorProvider modelSaveValidatorProvider,
            BreezeEntityValidator breezeEntityValidator,
            ISession session)
            : base(entityUpdater, saveWorkStateFactory, modelSaveValidatorProvider)
        {
            Session = session;
            BreezeEntityValidator = breezeEntityValidator;
        }

        protected override void BeforeFetchEntities(SaveChangesContext context)
        {
            BreezeEntityValidator.ValidateEntities(context.SaveMap, true);

            DataAnnotationsValidator.AddDescriptor(typeof(Customer), typeof(CustomerMetaData));
            var validator = new DataAnnotationsValidator();
            validator.ValidateEntities(context.SaveMap, true);
        }

        public IQueryable<Category> Categories => Session.Query<Category>();

        public IQueryable<Comment> Comments => Session.Query<Comment>();

        public IQueryable<Customer> Customers => Session.Query<Customer>();

        public IQueryable<Employee> Employees => Session.Query<Employee>();

        public IQueryable<Order> Orders => Session.Query<Order>();

        public IQueryable<OrderDetail> OrderDetails => Session.Query<OrderDetail>();

        public IQueryable<Product> Products => Session.Query<Product>();

        public IQueryable<Region> Regions => Session.Query<Region>();

        public IQueryable<Role> Roles => Session.Query<Role>();

        public IQueryable<Supplier> Suppliers => Session.Query<Supplier>();

        public IQueryable<Territory> Territories => Session.Query<Territory>();

        public IQueryable<TimeGroup> TimeGroups => Session.Query<TimeGroup>();

        public IQueryable<TimeLimit> TimeLimits => Session.Query<TimeLimit>();

        public IQueryable<UnusualDate> UnusualDates => Session.Query<UnusualDate>();

        public IQueryable<User> Users => Session.Query<User>();
    }
}
