using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Breeze.NHibernate.Internal;
using NHibernate.Engine;
using Cascade = Breeze.NHibernate.Internal.Cascade;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Represents a graph of dependencies between <see cref="EntityInfo"/>.
    /// </summary>
    public class DependencyGraph : IEnumerable<GraphNode>
    {
        private static readonly Dictionary<EntityState, CascadingAction> EntityStateCascadingActions =
            new Dictionary<EntityState, CascadingAction>
            {
                {EntityState.Added, CascadingAction.SaveUpdate},
                {EntityState.Modified, CascadingAction.SaveUpdate},
                {EntityState.Deleted, CascadingAction.Delete}
            };

        private readonly Dictionary<EntityInfo, GraphNode> _graph;

        /// <summary>
        /// Creates a new dependency graph.
        /// </summary>
        /// <param name="capacity">The initial capacity.</param>
        public DependencyGraph(int capacity)
        {
            _graph = new Dictionary<EntityInfo, GraphNode>(capacity);
        }

        /// <summary>
        /// Adds an <see cref="EntityInfo"/> to the graph.
        /// </summary>
        /// <param name="entityInfo"></param>
        public void AddToGraph(EntityInfo entityInfo)
        {
            TryAddToGraph(entityInfo, null, null);
        }

        /// <summary>
        /// Tries to add a child and its parent <see cref="EntityInfo"/> to the graph.
        /// </summary>
        /// <param name="child">The child entity info.</param>
        /// <param name="parent">The parent entity info.</param>
        /// <param name="association">The association between the parent and its child.</param>
        /// <returns>Whether the child and its parent were added to the graph.</returns>
        public bool TryAddToGraph(EntityInfo child, EntityInfo parent, EntityAssociation association)
        {
            var childNode = GetOrAdd(child);
            if (parent == null)
            {
                return false;
            }

            var parentNode = GetOrAdd(parent);
            return parentNode.AddChild(childNode, association) && childNode.AddParent(parentNode, association);
        }

        /// <summary>
        /// Gets the order in which the entities info will be saved. 
        /// </summary>
        /// <returns></returns>
        public List<EntityInfo> GetSaveOrder()
        {
            var processedNodes = new HashSet<GraphNode>();
            var saveOrder = new List<EntityInfo>(_graph.Count);
            var cascadeSaves = new List<Cascade>(_graph.Count);
            var cascadeDeletes = new List<Cascade>();
            foreach (var node in _graph.Values)
            {
                ProcessNode(node, processedNodes, cascadeSaves, cascadeDeletes);
            }

            // Sort cascades
            cascadeSaves.Sort();
            cascadeDeletes.Sort();

            saveOrder.AddRange(cascadeSaves.Select(o => o.Root.EntityInfo));
            saveOrder.AddRange(cascadeDeletes.Select(o => o.Root.EntityInfo));

            return saveOrder;
        }

        private static void ProcessNode(
            GraphNode node,
            HashSet<GraphNode> processedNodes,
            List<Cascade> cascadeSaves,
            List<Cascade> cascadeDeletes)
        {
            if (!processedNodes.Add(node))
            {
                return;
            }

            // Parents need to be the first to be saved
            foreach (var pair in node.Parents)
            {
                ProcessNode(pair.Key, processedNodes, cascadeSaves, cascadeDeletes);
            }

            if (!EntityStateCascadingActions.TryGetValue(node.EntityInfo.EntityState, out var cascadingAction))
            {
                return; // Do not add unchanged and detached entities to save
            }

            Cascade cascade;
            if (cascadingAction == CascadingAction.Delete)
            {
                cascade = new CascadeDelete(node, cascadeDeletes.Count);
                cascadeDeletes.Add(cascade);
            }
            else
            {
                cascade = new CascadeSave(node, cascadeSaves.Count);
                cascadeSaves.Add(cascade);
            }

            foreach (var pair in node.GetAllChildren(o => DoCascade(cascadingAction, o.Key, o.Value)))
            {
                cascade.AddChild(pair.Key);
            }
        }

        private static bool DoCascade(CascadingAction cascadingAction, GraphNode childNode, EntityAssociation childAssociation)
        {
            var cascade = childAssociation.InverseAssociationPropertyCascadeStyle;
            if (cascade == null)
            {
                return false;
            }

            return cascade.DoCascade(cascadingAction) &&
                   DoCascade(cascadingAction, childNode.EntityInfo.EntityState);
        }

        private static bool DoCascade(CascadingAction cascadingAction, EntityState childState)
        {
            // Deleted entities do not cascade with SaveUpdate action
            return (cascadingAction == CascadingAction.SaveUpdate && childState != EntityState.Deleted) ||
                   (cascadingAction == CascadingAction.Delete && childState == EntityState.Deleted);
        }
        /*
        public List<EntityInfo> GetSaveOrder2()
        {
            var processedNode = new HashSet<GraphNode>();
            var saveOrder = new List<EntityInfo>(_graph.Count);
            var deleteOrder = new List<EntityInfo>();

            foreach (var node in _graph.Values)
            {
                AddToSaveOrder(node, saveOrder, deleteOrder, processedNode);
            }

            deleteOrder.Reverse();
            saveOrder.AddRange(deleteOrder);

            return saveOrder;
        }

        private static void AddToSaveOrder(
            GraphNode node,
            List<EntityInfo> saveOrder,
            List<EntityInfo> deleteOrder,
            HashSet<GraphNode> processedNode)
        {
            if (!processedNode.Add(node))
            {
                return;
            }

            // Parents have to be the first in the save order
            foreach (var parent in node.Parents.Keys)
            {
                AddToSaveOrder(parent, saveOrder, deleteOrder, processedNode);
            }

            var entityInfo = node.EntityInfo;
            if (entityInfo.EntityState == EntityState.Deleted)
            {
                deleteOrder.Add(entityInfo);
            }
            else
            {
                saveOrder.Add(entityInfo);
            }
        }*/

        private GraphNode GetOrAdd(EntityInfo entityInfo)
        {
            if (_graph.TryGetValue(entityInfo, out var node))
            {
                return node;
            }

            node = new GraphNode(entityInfo);
            _graph.Add(entityInfo, node);

            return node;
        }

        /// <summary>
        /// Gets the enumerator that enumerates over added graph nodes.
        /// </summary>
        /// <returns>The graph nodes enumerator.</returns>
        public IEnumerator<GraphNode> GetEnumerator()
        {
            return _graph.Values.GetEnumerator();
        }

        /// <summary>
        /// Gets the enumerator that enumerates over added graph nodes.
        /// </summary>
        /// <returns>The graph nodes enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
