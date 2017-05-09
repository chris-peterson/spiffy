using System;

namespace Spiffy.Monitoring
{
    public interface ILoggingFacade
    {
        void Log(Level level, string message);
        void Initialize(Action<Level, string> logAction);
        void Initialize(LoggingBehavior behavior);
    }
}