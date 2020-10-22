using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Spiffy.Monitoring
{
    public static class Behavior
    {
        static readonly ConcurrentDictionary<string, Action<LogEvent>> LoggingActions = new ConcurrentDictionary<string, Action<LogEvent>>();

        /// <summary>
        /// Whether or not to remove newline characters from logged values.
        /// </summary>
        /// <returns>
        /// <code>true</code> if newline characters will be removed from logged
        /// values, <code>false</code> otherwise.
        /// </returns>
        public static bool RemoveNewlines { get; set; }

        public static void Initialize(Action<InitializationApi> customize)
        {
            if (customize == null)
            {
                throw new Exception("Configuration callback is required");
            }
            LoggingActions.Clear();
            customize(new InitializationApi());
        }

        internal static void AddLoggingAction(string id, Action<LogEvent> loggingAction)
        {
            LoggingActions.GetOrAdd(id, loggingAction);
        }

        internal static IList<Action<LogEvent>> GetLoggingActions()
        {
            return LoggingActions.Values.ToList();
        }
    }
}
