using System.Linq;
using Xunit;

namespace Breeze.NHibernate.Tests
{
    public class TestPersistenceManager : PersistenceManager
    {
        public TestPersistenceManager(
            EntityUpdater entityUpdater,
            ISaveWorkStateFactory saveWorkStateFactory,
            IModelSaveValidatorProvider modelSaveValidatorProvider)
            : base(entityUpdater, saveWorkStateFactory, modelSaveValidatorProvider)
        {
        }

        protected override void ValidateDependencyGraph(DependencyGraph dependencyGraph, SaveChangesContext context)
        {
            // Check that a child cannot be at the same time a parent for a node
            foreach (var node in dependencyGraph)
            {
                Assert.Empty(node.Parents.Select(o => o.Key.EntityInfo).Intersect(node.Children.Select(o => o.Key.EntityInfo)));
            }
        }
    }
}
