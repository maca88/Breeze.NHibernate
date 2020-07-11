using System;
using System.Collections.Generic;
using System.Linq;

namespace Breeze.NHibernate.Internal
{
    internal abstract class Cascade : IComparable<Cascade>
    {
        private static readonly HashSet<GraphNode> EmptySet = new HashSet<GraphNode>();

        private readonly bool _reverse;

        protected Cascade(GraphNode root, int index, bool reverse)
        {
            Root = root;
            Index = index;
            _reverse = reverse;
        }

        public GraphNode Root { get; }

        public int Index { get; }

        public HashSet<GraphNode> Children { get; private set; } = EmptySet;

        public void AddChild(GraphNode child)
        {
            if (Children == EmptySet)
            {
                Children = new HashSet<GraphNode>();
            }

            Children.Add(child);
        }

        public virtual int CompareTo(Cascade other)
        {
            // A root object that has no dependencies should be saved first in order
            // to avoid a "not-null property or transient value" exception when saving a parent
            // that cascades to children
            if (Root.Parents.Count == 0 && Root.Children.Count == 0)
            {
                return other.Root.Parents.Count == 0 && other.Root.Children.Count == 0
                    ? IndexCompareTo(other.Index)
                    : (_reverse ? 1 : -1);
            }

            if (other.Root.Parents.Count == 0 && other.Root.Children.Count == 0)
            {
                return Root.Parents.Count == 0 && Root.Children.Count == 0
                    ? IndexCompareTo(other.Index)
                    : (_reverse ? -1 : 1);
            }

            if (other.Children.Contains(Root) || Children.Any(o => o.Parents.ContainsKey(other.Root)))
            {
                return _reverse ? -1 : 1;
            }

            if (Children.Contains(other.Root) || other.Children.Any(o => o.Parents.ContainsKey(Root)))
            {
                return _reverse ? 1 : -1;
            }

            return IndexCompareTo(other.Index);
        }

        protected virtual int IndexCompareTo(int other)
        {
            return _reverse ? other.CompareTo(Index) : Index.CompareTo(other);
        }
    }
}
