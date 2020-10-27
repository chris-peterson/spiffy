using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Spiffy.Monitoring.Config
{
    public class InitializationApi
    {
        readonly ConcurrentDictionary<string, Action<LogEvent>> _providers = new ConcurrentDictionary<string, Action<LogEvent>>();

        // Custom providers should extend this API by way of extension methods
        public class ProvidersApi
        {
            readonly InitializationApi _parent;

            public ProvidersApi(InitializationApi parent)
            {
                _parent = parent;
            }

            public void Add(string id, Action<LogEvent> loggingAction)
            {
                _parent.AddProvider(id, loggingAction);
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

        void AddProvider(string id, Action<LogEvent> loggingAction)
        {
            _providers[id] = loggingAction;
        }

        internal Action<LogEvent> [] GetLoggingActions()
        {
            return _providers.Values.ToArray();
        }
    }
}