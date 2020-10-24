using System;
using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring
{
    public static class Behavior
    {
        static Action<LogEvent>[] _loggingActions = new Action<LogEvent>[] {};

        public static void Initialize(Action<InitializationApi> customize)
        {
            var api = new InitializationApi();
            if (customize == null)
            {
                throw new Exception("Configuration callback is required");
            }
            customize(api);

            _loggingActions = api.GetLoggingActions();
            RemoveNewLines = api.RemoveNewlines;
        }

        internal static Action<LogEvent> [] GetLoggingActions()
        {
            return _loggingActions;
        }

        internal static bool RemoveNewLines { get; set; }
    }
}
