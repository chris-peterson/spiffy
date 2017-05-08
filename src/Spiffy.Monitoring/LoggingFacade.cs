using System;
using System.Diagnostics;

namespace Spiffy.Monitoring
{
    public interface ILoggingFacade {
        void Log(Level level, string message);
        void Initialize(Action<Level, string> logAction);
        void Initialize(LoggingBehavior behavior);

    }

    public class LoggingFacade: ILoggingFacade
    {
        private LoggingFacade()
        {}

        static ILoggingFacade _instance;

        public static ILoggingFacade Instance
        {
            get { return _instance ?? (_instance = new LoggingFacade()); }
        }

        private Action<Level, string> _logAction;

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

    public enum LoggingBehavior
    {
        Trace,
        Console
    }
}