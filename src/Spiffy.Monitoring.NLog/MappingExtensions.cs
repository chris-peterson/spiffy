using NLog;

namespace Spiffy.Monitoring
{
    static class MappingExtensions
    {
        public static LogLevel ToNLogLevel(this Level level)
        {
            LogLevel nLogLevel;
            switch (level)
            {
                case Level.Info:
                    nLogLevel = LogLevel.Info;
                    break;
                case Level.Warning:
                    nLogLevel = LogLevel.Warn;
                    break;
                case Level.Error:
                    nLogLevel = LogLevel.Error;
                    break;
                default:
                    nLogLevel = LogLevel.Trace;
                    break;
            }
            return nLogLevel;
        }
    }
}