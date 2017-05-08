using System;
using System.Diagnostics;

namespace Spiffy.Monitoring
{
    public interface ILoggingFacade {
        void Log(Level level, string message);
        void Initialize(Action<Level, string> logAction);
        void Initialize(LoggingBehavior behavior);
    }

    public static class LoggingFacadeFactory {
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

    public class LoggingFacade : ILoggingFacade
    {
        protected Action<Level, string> _logAction;

        public virtual void Initialize(Action<Level, string> logAction)
        {
            _logAction = logAction;
        }

        public virtual void Initialize(LoggingBehavior behavior = LoggingBehavior.Console)
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

        public void Log(Level level, string message)
        {
            if (_logAction == null)
            {
                Initialize();
            }
            _logAction(level, message);
        }
    }

    public class DefaultLoggingFacade: LoggingFacade
    {
        private DefaultLoggingFacade()
        {}

        static ILoggingFacade _instance;

        public static ILoggingFacade Instance
        {
            get { return _instance ?? (_instance = LoggingFacadeFactory.Create()); }
        }
    }

    public enum LoggingBehavior
    {
        Trace,
        Console
    }
}