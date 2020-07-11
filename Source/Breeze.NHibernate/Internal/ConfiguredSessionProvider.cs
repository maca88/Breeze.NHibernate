using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;

namespace Breeze.NHibernate.Internal
{
    internal class ConfiguredSessionProvider : IDisposable
    {
        private readonly ISessionProvider _sessionProvider;
        private readonly Dictionary<Type, SessionInfo> _typeSessions = new Dictionary<Type, SessionInfo>();

        private class SessionInfo
        {
            public SessionInfo(ISession session, FlushMode originalFlushMode)
            {
                Session = session;
                OriginalFlushMode = originalFlushMode;
            }

            public ISession Session { get; }

            public FlushMode OriginalFlushMode { get; }
        }

        public ConfiguredSessionProvider(ISessionProvider sessionProvider)
        {
            _sessionProvider = sessionProvider;
        }

        public ISession GetSession(Type entityType)
        {
            if (_typeSessions.TryGetValue(entityType, out var sessionInfo))
            {
                return sessionInfo.Session;
            }

            var session = _sessionProvider.Get(entityType);
            if (session == null)
            {
                throw new InvalidOperationException($"Unknown entity type: {entityType}");
            }

            _typeSessions.Add(entityType, new SessionInfo(session, session.FlushMode));
            session.FlushMode = FlushMode.Manual;

            return session;
        }

        public IEnumerable<ISession> GetSessions() => _typeSessions.Values.Select(o => o.Session);

        public void Dispose()
        {
            foreach (var sessionInfo in _typeSessions.Values)
            {
                sessionInfo.Session.FlushMode = sessionInfo.OriginalFlushMode;
            }
        }
    }
}
