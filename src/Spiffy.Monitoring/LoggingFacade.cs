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

        public static void Log(Level level, string message)
        {
            if (_logAction == null)
            {
                Trace.WriteLine(message);
            }
            else
            {
                _logAction(level, message);
            }
        }
    }
}