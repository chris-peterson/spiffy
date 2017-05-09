using System;

namespace Spiffy.Monitoring
{
    public static class LoggingFacadeFactory 
    {
        public static ILoggingFacade Create(Action<Level, string> logAction)
        {
            var loggingFacade = new LoggingFacade();
            loggingFacade.Initialize(logAction);
            return loggingFacade;
        }

        public static ILoggingFacade Create(LoggingBehavior behavior)
        {
            var loggingFacade = new LoggingFacade();
            loggingFacade.Initialize(behavior);
            return loggingFacade;
        }

        public static ILoggingFacade Create()
        {
            var loggingFacade = new LoggingFacade();
            loggingFacade.Initialize();
            return loggingFacade;
        }
    }
}