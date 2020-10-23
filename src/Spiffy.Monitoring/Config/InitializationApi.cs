using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Spiffy.Monitoring.Config
{
    public class InitializationApi
    {
        readonly ConcurrentDictionary<string, Action<LogEvent>> _loggingActions = new ConcurrentDictionary<string, Action<LogEvent>>();

        // This class exists to provide a cohesive API across multiple providers (by way of extension methods)
        public class ProvidersApi
        {
            readonly InitializationApi _parent;

            public ProvidersApi(InitializationApi parent)
            {
                _parent = parent;
            }

            public void AddLoggingAction(string id, Action<LogEvent> loggingAction)
            {
                _parent.AddLoggingAction(id, loggingAction);
            }
        }

        public ProvidersApi Providers { get; }

        public InitializationApi()
        {
            Providers = new ProvidersApi(this);
        }
        
        /// <summary>
        /// Whether or not to remove newline characters from logged values.
        /// </summary>
        /// <returns>
        /// <code>true</code> if newline characters will be removed from logged
        /// values, <code>false</code> otherwise.
        /// </returns>
        public bool RemoveNewlines { get; set; }

        void AddLoggingAction(string id, Action<LogEvent> loggingAction)
        {
            _loggingActions.GetOrAdd(id, loggingAction);
        }

        internal ImmutableArray<Action<LogEvent>> GetLoggingActions()
        {
            return _loggingActions.Values.ToImmutableArray();
        }
    }
}