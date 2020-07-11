﻿using System;
using System.Collections.Generic;
using System.Linq;
using Breeze.NHibernate.Tests.Models;

namespace Breeze.NHibernate.Tests.Validators
{
    public class AggregateRootCollectionSaveValidator : ModelSaveValidator
    {
        protected override void ValidateRootNodes(Type rootModelType, List<GraphNode> rootNodes)
        {
            if (rootNodes.Count == 1)
            {
                base.ValidateRootNodes(rootModelType, rootNodes);
                return;
            }

            // Try to find the root node by IAggregate
            var aggregateRoots = new HashSet<object>();
            foreach (var node in rootNodes)
            {
                var entity = node.EntityInfo.Entity;
                if (entity is IAggregate aggregate)
                {
                    aggregateRoots.Add(aggregate.GetAggregateRoot());
                }
                else
                {
                    aggregateRoots.Add(node);
                }
            }

            foreach (var aggregateRoot in aggregateRoots)
            {
                var rootNode = rootNodes.Find(o => o.EntityInfo.Entity == aggregateRoot);
                if (rootNode == null)
                {
                    throw new InvalidOperationException("The aggregate root is not present in the dependency graph.");
                }

                if (rootNode.EntityInfo.EntityType != rootModelType)
                {
                    throw new InvalidOperationException("The found aggregate root type is not valid.");
                }
            }
        }
    }
}
