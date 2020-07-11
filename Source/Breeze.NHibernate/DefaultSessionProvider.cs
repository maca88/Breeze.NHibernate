using System;
using NHibernate;

namespace Breeze.NHibernate
{
    /// <summary>
    /// The default implementation for <see cref="ISessionProvider"/>.
    /// </summary>
    public class DefaultSessionProvider : ISessionProvider
    {
        private readonly Func<Type, ISession> _getFunction;

        public DefaultSessionProvider(Func<Type, ISession> getFunction)
        {
            _getFunction = getFunction;
        }

        /// <inheritdoc />
        public ISession Get(Type modelType)
        {
            return _getFunction(modelType);
        }
    }
}
