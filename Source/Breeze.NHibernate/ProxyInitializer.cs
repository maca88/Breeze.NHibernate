using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Breeze.NHibernate.Internal;
using NHibernate;
using NHibernate.Proxy;

namespace Breeze.NHibernate
{
    /// <inheritdoc />
    public class ProxyInitializer : IProxyInitializer
    {
        private static readonly char[] Delimiters = { '/', '.' };

        private readonly ConcurrentDictionary<Type, TypeExpandMetadata> _typeExpandMetadata =
            new ConcurrentDictionary<Type, TypeExpandMetadata>();

        /// <inheritdoc />
        public void Initialize(object value, IEnumerable<string> expandPaths)
        {
            var rootExpandNode = GetRootExpandNode(expandPaths);
            if (rootExpandNode.Expands.Count == 0)
            {
                return;
            }

            Initialize(value, rootExpandNode);
        }

        private void Initialize(object value, ExpandNode expandNode)
        {
            if (value is IEnumerable enumerable)
            {
                InitializeEnumerable(enumerable, expandNode);
            }
            else
            {
                InitializeObject(value, expandNode);
            }
        }

        private void InitializeEnumerable(IEnumerable items, ExpandNode expandNode)
        {
            NHibernateUtil.Initialize(items);
            foreach (var item in items)
            {
                InitializeObject(item, expandNode);
            }
        }

        private void InitializeObject(object item, ExpandNode expandNode)
        {
            if (item is INHibernateProxy nhProxy)
            {
                nhProxy.HibernateLazyInitializer.Initialize();
                item = nhProxy.HibernateLazyInitializer.GetImplementation();
            }

            var metadata = _typeExpandMetadata.GetOrAdd(BreezeHelper.GetEntityType(item), CreateExpandMetadata);
            foreach (var expand in expandNode.Expands.Values)
            {
                Initialize(metadata.GetPropertyValue(expand.PropertyName, item), expand);
            }
        }

        private static TypeExpandMetadata CreateExpandMetadata(Type type)
        {
            return new TypeExpandMetadata(type);
        }

        private static RootExpandNode GetRootExpandNode(IEnumerable<string> expandPaths)
        {
            var expandTree = new RootExpandNode();
            foreach (var expandPath in expandPaths)
            {
                var paths = expandPath.Split(Delimiters);
                AddExpandPaths(paths, expandTree);
            }

            return expandTree;
        }

        private static void AddExpandPaths(string[] paths, RootExpandNode rootExpandNode)
        {
            var currentNode = rootExpandNode.GetOrAdd(paths[0]);
            for (var i = 1; i < paths.Length; i++)
            {
                var path = paths[i];
                currentNode = currentNode.GetOrAdd(path);
            }
        }

        private class TypeExpandMetadata
        {
            private readonly ConcurrentDictionary<string, Func<object, object>> _propertyGetters =
                new ConcurrentDictionary<string, Func<object, object>>();

            public TypeExpandMetadata(Type type)
            {
                Type = type;
            }

            public Type Type { get; }

            public object GetPropertyValue(string propertyName, object value)
            {
                return _propertyGetters.GetOrAdd(propertyName, CreatePropertyGetter)(value);
            }

            private Func<object, object> CreatePropertyGetter(string propertyName)
            {
                var property = Type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (property == null)
                {
                    throw new InvalidOperationException($"Property {propertyName} does not exist on type {Type}");
                }

                var parameter = Expression.Parameter(typeof(object));
                return Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                        Expression.MakeMemberAccess(Expression.Convert(parameter, Type), property),
                        typeof(object)),
                    parameter
                ).Compile();
            }
        }

        private class RootExpandNode : ExpandNode
        {
            public RootExpandNode() : base(null)
            {
            }
        }

        private class ExpandNode
        {
            public ExpandNode(string propertyName)
            {
                PropertyName = propertyName;
            }

            public string PropertyName { get; }

            public Dictionary<string, ExpandNode> Expands { get; } = new Dictionary<string, ExpandNode>();

            public ExpandNode GetOrAdd(string memberName)
            {
                if (!Expands.TryGetValue(memberName, out var node))
                {
                    node = new ExpandNode(memberName);
                    Expands.Add(node.PropertyName, node);
                }

                return node;
            }
        }
    }
}
