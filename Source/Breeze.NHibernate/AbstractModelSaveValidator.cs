using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Engine;

namespace Breeze.NHibernate
{
    /// <summary>
    /// An abstract model validator that validates a save operation based on the <see cref="DependencyGraph"/> and
    /// the <see cref="CascadeStyle"/> of the related associations. A child can be saved only if a bi-directional association exists
    /// and the association <see cref="CascadeStyle"/> allows such operation.
    /// </summary>
    public abstract class AbstractModelSaveValidator : IModelSaveValidator
    {
        private static readonly HashSet<CascadeStyle> ValidCascadeStyles = new HashSet<CascadeStyle>
        {
            CascadeStyle.All, CascadeStyle.AllDeleteOrphan
        };

        private static readonly Dictionary<EntityState, CascadeStyle> StateValidCascadeStyles =
            new Dictionary<EntityState, CascadeStyle>
            {
                {EntityState.Added, CascadeStyle.Update},
                {EntityState.Modified, CascadeStyle.Update},
                {EntityState.Deleted, CascadeStyle.Delete}
            };

        /// <inheritdoc />
        public IEnumerable<EntityInfo> Validate(Type rootModelType, DependencyGraph dependencyGraph)
        {
            var rootNodes = dependencyGraph.Where(o => ShouldValidateNode(o) && o.Parents.Count == 0).ToList();
            if (rootNodes.Count == 0)
            {
                throw new InvalidOperationException("There is no root node.");
            }

            ValidateRootNodes(rootModelType, rootNodes);
            ValidateGraph(dependencyGraph);

            return rootNodes.Select(o => o.EntityInfo);
        }

        /// <summary>
        /// Validates the given <see cref="DependencyGraph"/>.
        /// </summary>
        /// <param name="dependencyGraph">The graph to validate.</param>
        /// <exception cref="InvalidOperationException">Thrown when there is no bi-directional relation between a child and it parent or
        /// the association <see cref="CascadeStyle"/> does not permit such operation.</exception>
        protected virtual void ValidateGraph(DependencyGraph dependencyGraph)
        {
            foreach (var node in dependencyGraph)
            {
                if (!ShouldValidateNode(node))
                {
                    continue;
                }

                foreach (var pair in node.Children)
                {
                    var childNode = pair.Key;
                    if (!ShouldValidateNode(childNode))
                    {
                        continue;
                    }

                    var cascade = pair.Value.InverseAssociationPropertyCascadeStyle;
                    if (cascade == null)
                    {
                        throw new InvalidOperationException($"There is no bi-directional relation between {childNode.EntityInfo.EntityType} and {node.EntityInfo.EntityType}.");
                    }

                    if (IsCascaded(childNode.EntityInfo.EntityState, cascade))
                    {
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"Mapped cascade for property {node.EntityInfo.EntityType}.{pair.Value.InverseAssociationPropertyName} does not allow {GetOperationName(childNode.EntityInfo)} operation");
                }
            }
        }

        /// <summary>
        /// Check whether the given <see cref="GraphNode"/> should be validated.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>Whether the <see cref="GraphNode"/> should be validated.</returns>
        protected virtual bool ShouldValidateNode(GraphNode node)
        {
            return true;
        }

        /// <summary>
        /// Validates the root nodes.
        /// </summary>
        /// <param name="rootModelType">The root model type.</param>
        /// <param name="rootNodes">List of root nodes.</param>
        protected abstract void ValidateRootNodes(Type rootModelType, List<GraphNode> rootNodes);

        private static bool IsCascaded(EntityState state, CascadeStyle cascade)
        {
            if (cascade == null || cascade == CascadeStyle.None)
            {
                return false;
            }

            return ValidCascadeStyles.Contains(cascade) ||
                   StateValidCascadeStyles.TryGetValue(state, out var validCascade) &&
                   validCascade == cascade;
        }

        private static string GetOperationName(EntityInfo entityInfo)
        {
            switch (entityInfo.EntityState)
            {
                case EntityState.Added:
                    return "Create";
                case EntityState.Modified:
                    return "Update";
                case EntityState.Deleted:
                    return "Delete";
                default:
                    return "Unknown";
            }
        }
    }
}
