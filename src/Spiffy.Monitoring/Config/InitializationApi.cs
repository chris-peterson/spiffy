using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Spiffy.Monitoring.Config
{
    public class InitializationApi
    {
        readonly ConcurrentDictionary<string, Action<LogEvent>> _providers = new();
        readonly ConcurrentBag<Action<EventContext>> _beforeLoggingCallbacks = new();

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

        public class CallbacksApi
        {
            readonly InitializationApi _parent;

            public CallbacksApi(InitializationApi parent)
            {
                _parent = parent;
            }

            public void BeforeLogging(Action<EventContext> action)
            {
                _parent.AddBeforeLoggingCallback(action);
            }
        }

        public ProvidersApi Providers { get; }
        public CallbacksApi Callbacks { get; }

        public InitializationApi()
        {
            Providers = new ProvidersApi(this);
            Callbacks = new CallbacksApi(this);
        }

        /// <summary>
        /// If set, this value is used for logging values that are null.
        /// </summary>
        public string CustomNullValue { get; set; }

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

        void AddBeforeLoggingCallback(Action<EventContext> action)
        {
            _beforeLoggingCallbacks.Add(action);
        }

        internal Action<EventContext> [] GetBeforeLoggingActions()
        {
            return _beforeLoggingCallbacks.ToArray();
        }
    }
}
