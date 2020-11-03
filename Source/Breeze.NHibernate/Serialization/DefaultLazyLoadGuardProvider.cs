using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Breeze.NHibernate.Extensions;
using Breeze.NHibernate.Internal;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Intercept;
using NHibernate.Proxy;

namespace Breeze.NHibernate.Serialization
{
    /// <summary>
    /// The default implementation for <see cref="ILazyLoadGuardProvider"/>. Adds a guard that compares <see cref="IPersistenceContext.CollectionsByKey"/> and
    /// <see cref="IPersistenceContext.EntitiesByKey"/> before and after calling the member getter. As <see cref="ILazyLoadGuardProvider"/> is registered as a
    /// singleton, the provider tries to find the <see cref="ISessionImplementor"/> from a proxy that is located in the serializing object, which is not deeper
    /// than <see cref="MaxDepth"/>.
    /// </summary>
    public class DefaultLazyLoadGuardProvider : ILazyLoadGuardProvider
    {
        private delegate ISessionImplementor GetSessionDelegate(object value, int depth, Func<object, int, bool> canContinuePredicate);

        private static readonly Func<AbstractPersistentCollection, ISessionImplementor> GetCollectionSessionFunc;
        static DefaultLazyLoadGuardProvider()
        {
            var sessionProperty = typeof(AbstractPersistentCollection).GetProperty("Session", BindingFlags.NonPublic | BindingFlags.Instance);
            if (sessionProperty == null)
            {
                throw new InvalidOperationException("AbstractPersistentCollection does not have Session property.");
            }

            var parameter = Expression.Parameter(typeof(AbstractPersistentCollection));
            GetCollectionSessionFunc = Expression.Lambda<Func<AbstractPersistentCollection, ISessionImplementor>>(
                Expression.MakeMemberAccess(parameter, sessionProperty),
                parameter
            ).Compile();
        }

        private readonly IEntityMetadataProvider _entityMetadataProvider;
        private readonly ConcurrentDictionary<Type, GetSessionDelegate> _typeSessionGetters = new ConcurrentDictionary<Type, GetSessionDelegate>();
        private readonly ObjectPool<HashSet<object>> _hashSetPool = new ObjectPool<HashSet<object>>(() => new HashSet<object>());

        /// <summary>
        /// Constructs an instance of <see cref="DefaultLazyLoadGuardProvider"/>.
        /// </summary>
        public DefaultLazyLoadGuardProvider(IEntityMetadataProvider entityMetadataProvider)
        {
            _entityMetadataProvider = entityMetadataProvider;
        }

        /// <summary>
        /// The maximum depth to search for <see cref="ISessionImplementor"/>. Default is 2.
        /// </summary>
        public int MaxDepth { get; set; } = 2;

        /// <inheritdoc />
        public virtual Func<object, object> AddGuard(Func<object, object> getValueFunction, Type memberReflectedType, string memberName)
        {
            var sessionGetters = GetOrCreateTypeSessionGetter(memberReflectedType);
            return GetValue;

            object GetValue(object target)
            {
                return !TryGetSession(target, sessionGetters, out var session)
                    ? getValueFunction(target)
                    : GetValueGuarded(getValueFunction, memberReflectedType, memberName, session, target);
            }
        }

        /// <summary>
        /// Gets the value from the provided member getter while guarding whether for lazy loads.
        /// </summary>
        /// <param name="getValueFunction">The member getter.</param>
        /// <param name="memberReflectedType">The reflected type of the memeber.</param>
        /// <param name="memberName">The member name.</param>
        /// <param name="session">The NHibernate session.</param>
        /// <param name="target">The instance to get the member value from.</param>
        /// <returns>The member value.</returns>
        protected static object GetValueGuarded(Func<object, object> getValueFunction, Type memberReflectedType, string memberName, ISessionImplementor session, object target)
        {
            var totalObjects = GetTotalObjects(session);
            try
            {
                return getValueFunction(target);
            }
            catch (JsonSerializationException ex)
            {
                if (ex.InnerException is LazyInitializationException)
                {
                    // Happens for non mapped computed properties that touch uninitialized relations when session is closed
                    throw new InvalidOperationException(
                        $"A lazy load occurred when serializing property '{memberName}' from type {memberReflectedType}. " +
                        "To fix this issue set a predicate with IMemberConfigurator.ShouldSerialize method that checks " +
                        "whether all association properties used in the getter are initialized.");
                }

                throw;
            }
            finally
            {
                if (totalObjects != GetTotalObjects(session))
                {
                    throw new InvalidOperationException(
                        $"A lazy load occurred when serializing property '{memberName}' from type {memberReflectedType}. " +
                        "To fix this issue set a predicate with IMemberConfigurator.ShouldSerialize method that checks " +
                        "whether all association properties used in the getter are initialized.");
                }
            }
        }

        private bool TryGetSession(object target, GetSessionDelegate getSessionFunc, out ISessionImplementor session)
        {
            var traversedObjects = _hashSetPool.Get();
            try
            {
                session = getSessionFunc(target, 0, (value, depth) => CanContinue(value, depth, traversedObjects));
                return session != null;
            }
            finally
            {
                traversedObjects.Clear();
                _hashSetPool.Return(traversedObjects);
            }
        }

        private bool CanContinue(object value, int depth, HashSet<object> traversedObjects)
        {
            return depth <= MaxDepth && traversedObjects.Add(value);
        }

        private GetSessionDelegate GetOrCreateTypeSessionGetter(Type reflectedType)
        {
            return _typeSessionGetters.GetOrAdd(reflectedType, CreateTypeSessionGetter);
        }

        private bool IsEntityType(Type type)
        {
            return _entityMetadataProvider.IsEntityType(type);
        }

        private GetSessionDelegate CreateTypeSessionGetter(Type reflectedType)
        {
            // Find all auto properties and fields that can contain a proxy
            var properties = reflectedType
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(o => o.IsAutoProperty(true)); // for non auto properties we will check the underlying field instead
            var fields = reflectedType
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(o => !o.Name.StartsWith("<")); // Ignore compiler generated fields

            var list = new List<GetSessionDelegate>();
            foreach (var member in properties.Cast<MemberInfo>().Concat(fields))
            {
                if (!BreezeContractResolver.IsAssociation(member, _entityMetadataProvider, out var isScalar, out var memberType))
                {
                    continue;
                }

                var parameter = Expression.Parameter(typeof(object));
                var getValueFunc = Expression.Lambda<Func<object, object>>(
                        Expression.Convert(
                            Expression.MakeMemberAccess(Expression.Convert(parameter, reflectedType), member),
                            typeof(object)),
                        parameter)
                    .Compile();

                list.Add(isScalar 
                    ? CreateGetSessionFromEntityFunction(getValueFunc, GetOrCreateTypeSessionGetter)
                    : CreateGetSessionFromCollectionFunction(getValueFunc, memberType, GetOrCreateTypeSessionGetter, IsEntityType));
            }

            return CreateTypeSessionGetter(list.ToArray());
        }

        private static GetSessionDelegate CreateTypeSessionGetter(GetSessionDelegate[] getters)
        {
            var length = getters.Length;
            return GetSessionFromObject;

            ISessionImplementor GetSessionFromObject(object value, int depth, Func<object, int, bool> predicate)
            {
                var session = GetSession(value, out var isProxy);
                if (session != null || isProxy)
                {
                    return session;
                }

                for (var i = 0; i < length; i++)
                {
                    session = getters[i](value, depth, predicate);
                    if (session != null)
                    {
                        return session;
                    }
                }

                return null;
            }
        }

        private static GetSessionDelegate CreateGetSessionFromEntityFunction(
            Func<object, object> getValueFunc,
            Func<Type, GetSessionDelegate> getTypeSessionGetter)
        {
            return GetSessionFromEntityMember;

            ISessionImplementor GetSessionFromEntityMember(object model, int depth, Func<object, int, bool> predicate)
            {
                var value = getValueFunc(model);
                return GetSessionFromEntity(value, depth, predicate, getTypeSessionGetter);
            }
        }

        private static ISessionImplementor GetSessionFromEntity(
            object value,
            int depth,
            Func<object, int, bool> predicate,
            Func<Type, GetSessionDelegate> getTypeSessionGetter)
        {
            return GetSession(value, out var isProxy) ?? (isProxy ? null : GetSessionFromEntityRecursive());

            ISessionImplementor GetSessionFromEntityRecursive()
            {
                if (!predicate(value, depth) || value == null)
                {
                    return null;
                }

                var sessionGetter = getTypeSessionGetter(BreezeHelper.GetEntityType(value, false));
                return sessionGetter.Invoke(value, depth + 1, predicate);
            }
        }

        private static GetSessionDelegate CreateGetSessionFromCollectionFunction(
            Func<object, object> getValueFunc,
            Type memberType,
            Func<Type, GetSessionDelegate> getTypeSessionGetter,
            Predicate<Type> isEntityPredicate)
        {
            return GetSessionFromCollectionMember;

            ISessionImplementor GetSessionFromCollectionMember(object model, int depth, Func<object, int, bool> predicate)
            {
                var collection = (IEnumerable)getValueFunc(model);
                return memberType != null
                    ? GetSessionFromCollection(collection, depth, predicate, getTypeSessionGetter, memberType)
                    : GetSessionFromCollection(collection, depth, predicate, getTypeSessionGetter, isEntityPredicate);
            }
        }

        private static ISessionImplementor GetSessionFromCollection(IEnumerable collection,
            int depth,
            Func<object, int, bool> predicate,
            Func<Type, GetSessionDelegate> getTypeSessionGetter,
            Type memberType)
        {
            return GetSession(collection, out var isProxy) ?? (isProxy ? null : GetSessionFromCollection());

            ISessionImplementor GetSessionFromCollection()
            {
                if (!predicate(collection, depth) || collection == null)
                {
                    return null;
                }

                depth++;
                // Generic collection
                var sessionGetter = getTypeSessionGetter(memberType);
                foreach (var item in collection)
                {
                    var session = sessionGetter(item, depth, predicate);
                    if (session != null)
                    {
                        return session;
                    }
                }

                return null;
            }
        }

        private static ISessionImplementor GetSessionFromCollection(IEnumerable collection,
            int depth,
            Func<object, int, bool> predicate,
            Func<Type, GetSessionDelegate> getTypeSessionGetter,
            Predicate<Type> isEntityPredicate)
        {
            return GetSession(collection, out var isProxy) ?? (isProxy ? null : GetSessionFromCollection());

            ISessionImplementor GetSessionFromCollection()
            {
                if (!predicate(collection, depth) || collection == null)
                {
                    return null;
                }

                depth++;
                // Non generic collection
                foreach (var item in collection)
                {
                    var type = BreezeHelper.GetEntityType(item, false);
                    if (isEntityPredicate(type))
                    {
                        continue;
                    }

                    var session = GetSessionFromEntity(item, depth, predicate, getTypeSessionGetter);
                    if (session != null)
                    {
                        return session;
                    }
                }

                return null;
            }
        }

        private static ISessionImplementor GetSession(object value, out bool isProxy)
        {
            switch (value)
            {
                case INHibernateProxy nhProxy:
                    isProxy = true;
                    return nhProxy.HibernateLazyInitializer.Session;
                case IFieldInterceptorAccessor fieldInterceptor:
                    isProxy = true;
                    return fieldInterceptor.FieldInterceptor.Session;
                default:
                    isProxy = false;
                    return null;
            }
        }

        private static ISessionImplementor GetSession(IEnumerable collection, out bool isProxy)
        {
            switch (collection)
            {
                case AbstractPersistentCollection persistentCollection:
                    isProxy = true;
                    return GetCollectionSessionFunc(persistentCollection);
                default:
                    isProxy = false;
                    return null;
            }
        }

        private static int GetTotalObjects(ISessionImplementor session)
        {
            return session.PersistenceContext.CollectionsByKey.Count +
                   session.PersistenceContext.EntitiesByKey.Count;
        }
    }
}
