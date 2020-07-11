using System;
using System.Collections.Generic;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A model save validator that allows only one root model.
    /// </summary>
    public class ModelSaveValidator : AbstractModelSaveValidator
    {
        /// <inheritdoc />
        protected override void ValidateRootNodes(Type rootModelType, List<GraphNode> rootNodes)
        {
            if (rootNodes.Count > 1)
            {
                throw new InvalidOperationException("Multiple root nodes were found.");
            }

            if (rootNodes[0].EntityInfo.EntityType != rootModelType)
            {
                throw new InvalidOperationException("The found root type is not valid.");
            }
        }
    }
}
