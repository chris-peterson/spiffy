using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Spiffy.Monitoring
{
    public static class NLog
    {
        private const string LoggerName = "Spiffy";
        private static Logger _logger;

        public static void Initialize()
        {
            if (_logger != null) 
                return;

            _logger = SetupNLog();

            LoggingFacade.Initialize((level, message) =>
            {
                LogLevel nLogLevel = LogLevel.Trace;
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

        private static Logger SetupNLog()
        {
            var fileTarget = new FileTarget
            {
                Name = "FileTarget",
                Layout = "${message}",
                ConcurrentWrites = false,
                FileName = new SimpleLayout("${basedir}/Logs/current.log"),
                ArchiveEvery = FileArchivePeriod.Day,
                ArchiveNumbering = ArchiveNumberingMode.Sequence,
                MaxArchiveFiles = 2,
                ArchiveFileName = new SimpleLayout("${basedir}/Logs/archive/{####}.log")
            };
            var asyncWrapper = new AsyncTargetWrapper(fileTarget)
            {
                Name = "AsyncWrapper"
            };

            var config = new LoggingConfiguration();
            config.AddTarget(LoggerName, asyncWrapper);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, asyncWrapper));

            LogManager.Configuration = config;

            return LogManager.GetLogger(LoggerName);
        }
    }
}
