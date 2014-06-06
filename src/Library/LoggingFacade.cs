using System;
using System.Diagnostics;

namespace Spiffy.Monitoring
{
    public static class LoggingFacade
    {
        private static Action<string> _logAction;

        public static void Initialize(Action<string> logAction)
        {
            _logAction = logAction;
        }

        public static void Log(string message)
        {
            if (_logAction == null)
            {
                Trace.WriteLine(message);
            }
            else
            {
                _logAction(message);
            }
        }
    }
}