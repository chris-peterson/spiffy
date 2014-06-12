using NLog;

namespace Spiffy.Monitoring.NLog
{
    public static class Bootstrapper
    {
        private static Logger _logger;

        public static void Initialize()
        {
            if (_logger == null)
            {
                _logger = LogManager.GetLogger("Default");
                
                LoggingFacade.Initialize((level, message) =>
                {
                    LogLevel nLogLevel = LogLevel.Debug;
                    switch (level)
                    {
                        case Level.Info:
                            nLogLevel = LogLevel.Info;
                            break;
                        case Level.Error:
                            nLogLevel = LogLevel.Error;
                            break;
                    }
                    _logger.Log(nLogLevel, message);

                });
            }
        }
    }
}
