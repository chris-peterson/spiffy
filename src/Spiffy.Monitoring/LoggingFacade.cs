using System;
using System.Diagnostics;

namespace Spiffy.Monitoring
{
    public static class LoggingFacade
    {
        private static Action<Level, string> _logAction;

        public static void Initialize(Action<Level, string> logAction)
        {
            _logAction = logAction;
        }

        public static void Initialize(LoggingBehavior behavior = LoggingBehavior.Console)
        {
            switch (behavior)
            {
                case LoggingBehavior.Console:
                    _logAction = (level, message) => Console.WriteLine(message);
                    break;
                case LoggingBehavior.Trace:
                    _logAction = (level, message) => Trace.WriteLine(message);
                    break;
                default:
                    throw new NotSupportedException($"{behavior} is not supported");
            }
        }

        public static void Log(Level level, string message)
        {
            if (_logAction == null)
            {
                Initialize();
            }
            _logAction(level, message);
        }
    }

    public enum LoggingBehavior
    {
        Trace,
        Console
    }
}