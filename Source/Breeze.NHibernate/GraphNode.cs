using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Represents a node inside a <see cref="DependencyGraph"/>, which contain an <see cref="EntityInfo"/> with its children and parents.
    /// </summary>
    public class GraphNode
    {
        private static readonly IReadOnlyDictionary<GraphNode, EntityAssociation> EmptyDictionary
            = new ReadOnlyDictionary<GraphNode, EntityAssociation>(new Dictionary<GraphNode, EntityAssociation>(0));

        private Dictionary<GraphNode, EntityAssociation> _children;
        private Dictionary<GraphNode, EntityAssociation> _parents;

        /// <summary>
        /// Constructs an instance of <see cref="GraphNode"/>.
        /// </summary>
        public GraphNode(EntityInfo entityInfo)
        {
            EntityInfo = entityInfo;
        }

        /// <summary>
        /// The entity info.
        /// </summary>
        public EntityInfo EntityInfo { get; }

        /// <summary>
        /// A dictionary of children and their associations. 
        /// </summary>
        public IReadOnlyDictionary<GraphNode, EntityAssociation> Children => _children ?? EmptyDictionary;

        /// <summary>
        /// A dictionary of parents and their associations. 
        /// </summary>
        public IReadOnlyDictionary<GraphNode, EntityAssociation> Parents => _parents ?? EmptyDictionary;

        internal IEnumerable<KeyValuePair<GraphNode, EntityAssociation>> GetAllChildren(Predicate<KeyValuePair<GraphNode, EntityAssociation>> predicate)
        {
            var traversedNodes = new HashSet<GraphNode>();
            return GetAllChildren(traversedNodes, predicate);
        }

        private IEnumerable<KeyValuePair<GraphNode, EntityAssociation>> GetAllChildren(
            HashSet<GraphNode> traversedNodes,
            Predicate<KeyValuePair<GraphNode, EntityAssociation>> predicate)
        {
            foreach (var pair in Children)
            {
                if (!traversedNodes.Add(pair.Key) || !predicate(pair))
                {
                    continue;
                }

                yield return pair;
            }

            foreach (var pair in Children)
            {
                foreach (var subPair in pair.Key.GetAllChildren(traversedNodes, predicate))
                {
                    if (!traversedNodes.Add(pair.Key) || !predicate(pair))
                    {
                        continue;
                    }

                    yield return subPair;
                }
            }
        }

        internal void AddParent(GraphNode parent, EntityAssociation association)
        {
            if (_parents == null)
            {
                _parents = new Dictionary<GraphNode, EntityAssociation>();
            }
            
            _parents.Add(parent, association);
        }

        internal void AddChild(GraphNode child, EntityAssociation association)
        {
            if (_children == null)
            {
                _children = new Dictionary<GraphNode, EntityAssociation>();
            }

            _children.Add(child, association);
        }
    }
}
