
namespace Breeze.NHibernate.Internal
{
    internal class CascadeDelete : Cascade
    {
        public CascadeDelete(GraphNode root, int index) : base(root, index, true)
        {
        }
    }
}
