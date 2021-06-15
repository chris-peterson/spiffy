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
        
        /// <summary>
        /// Values over this length will be deprioritized in the <see cref="LogEvent.Message"/>.
        /// Defaults to 1024.
        /// </summary>
        /// <remarks>
        /// In some logging scenarios, long values can result in some key/value pairs being cut off.
        /// Key/value pairs with values whose length exceeds this value will be output after those
        /// pairs whose values do not.
        /// </remarks>
        public int DeprioritizedValueLength { get; set; } = 1024;

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