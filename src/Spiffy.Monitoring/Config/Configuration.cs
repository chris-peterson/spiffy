using System;
using Spiffy.Monitoring.Config;

// ReSharper disable once CheckNamespace -- this is intentional to avoid additional nesting
namespace Spiffy.Monitoring
{
    public static class Configuration
    {
        static Action<EventContext>[] _beforeLoggingActions = new Action<EventContext>[] {};
        static Action<LogEvent>[] _loggingActions = new Action<LogEvent>[] {};

        public static void Initialize(Action<InitializationApi> customize)
        {
            var api = new InitializationApi();
            if (customize == null)
            {
                throw new Exception("Configuration callback is required");
            }
            customize(api);

            _beforeLoggingActions = api.GetBeforeLoggingActions();
            _loggingActions = api.GetLoggingActions();
            RemoveNewLines = api.RemoveNewlines;
            DeprioritizedValueLength = api.DeprioritizedValueLength;
        }

        internal static Action<EventContext> [] GetBeforeLoggingActions()
        {
            return _beforeLoggingActions;
        }

        internal static Action<LogEvent> [] GetLoggingActions()
        {
            return _loggingActions;
        }

        internal static bool RemoveNewLines { get; private set; }

        internal static int DeprioritizedValueLength { get; private set; } = 1024;
    }
}
