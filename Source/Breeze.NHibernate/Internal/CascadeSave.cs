
namespace Breeze.NHibernate.Internal
{
    internal class CascadeSave : Cascade
    {
        public CascadeSave(GraphNode root, int index) : base(root, index, false)
        {
        }
    }
}
