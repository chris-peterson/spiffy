using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Spiffy.Monitoring
{
    public static class Behavior
    {
        static readonly List<Action<LogEvent>> _loggingActions = new List<Action<LogEvent>>();

        /// <summary>
        /// Whether or not to remove newline characters from logged values.
        /// </summary>
        /// <returns>
        /// <code>true</code> if newline characters will be removed from logged
        /// values, <code>false</code> otherwise.
        /// </returns>
        public static bool RemoveNewlines { get; set; }

        public static void AddBuiltInLogging(BuiltInLogging behavior)
        {
            switch (behavior)
            {
                case Monitoring.BuiltInLogging.Console:
                    _loggingActions.Add(logEvent =>
                    {
                        if (logEvent.Level == Level.Error)
                        {
                            Console.Error.WriteLine(logEvent.MessageWithTime);
                        }
                        else
                        {
                            Console.WriteLine(logEvent.MessageWithTime);
                        }
                    });
                    break;
                case Monitoring.BuiltInLogging.Trace:
                    _loggingActions.Add(logEvent =>
                    {
                        var message = logEvent.Message;
                        switch (logEvent.Level)
                        {
                            case Level.Info:
                                Trace.TraceInformation(message);
                                break;
                            case Level.Warning:
                                Trace.TraceWarning(message);
                                break;
                            case Level.Error:
                                Trace.TraceError(message);
                                break;
                            default:
                                Trace.WriteLine(message);
                                break;
                        }
                    });
                    break;
                default:
                    throw new NotSupportedException($"{behavior} is not supported");
            }
        }

        public static void AddCustomLogging(Action<LogEvent> loggingAction)
        {
            _loggingActions.Add(loggingAction);
        }

        internal static IList<Action<LogEvent>> GetLoggingActions()
        {
            return _loggingActions;
        }
    }

    public enum BuiltInLogging
    {
        Trace,
        Console
    }
}
