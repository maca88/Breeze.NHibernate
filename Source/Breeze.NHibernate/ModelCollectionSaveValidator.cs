using System;
using System.Collections.Generic;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A model validator that allows save operation for multiple roots of the same type.
    /// </summary>
    public class ModelCollectionSaveValidator : AbstractModelSaveValidator
    {
        /// <inheritdoc />
        protected override void ValidateRootNodes(Type rootModelType, List<GraphNode> rootNodes)
        {
            foreach (var rootNode in rootNodes)
            {
                if (rootNode.EntityInfo.EntityType != rootModelType)
                {
                    throw new InvalidOperationException("The found root type is not valid.");
                }
            }
        }
    }
}
