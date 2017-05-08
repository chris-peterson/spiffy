using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Spiffy.Monitoring
{
    public static class NLogFactory
    {
        public static ILoggingFacade Create(Action<NLogConfigurationApi> configure = null, string name = null) 
        {
            var config = new NLogConfigurationApi();
            if(null != configure)
            {
                configure(config);
            }

            var logger = NLog.SetupNLog(config, name);

            var loggingFacade = LoggingFacadeFactory.Create((level, message) =>
            {
                var nLogLevel = NLog.LevelToNLogLevel(level);
                logger.Log(nLogLevel, message);
            });

            return loggingFacade;
        }

    }
    public static class NLog
    {
        private const string LoggerName = "Spiffy";
        private static Logger _logger;

        public static void Initialize(Action<NLogConfigurationApi> configure = null)
        {
            if (_logger != null) 
                return;

            var config = new NLogConfigurationApi();
            if (configure != null)
            {
                configure(config);
            }

            _logger = SetupNLog(config);

            DefaultLoggingFacade.Instance.Initialize((level, message) =>
            {
                var nLogLevel = LevelToNLogLevel(level);
                _logger.Log(nLogLevel, message);
            });
        }

        internal static LogLevel LevelToNLogLevel(Level level)
        {
            var nLogLevel = LogLevel.Trace;
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
            }
            return nLogLevel;
        }

        internal static Logger SetupNLog(NLogConfigurationApi config, string loggerName = null)
        {
            loggerName = loggerName ?? LoggerName;

            var logDirectory = string.IsNullOrEmpty(config.LogDirectory) ? 
                "${basedir}/Logs" : 
                config.LogDirectory;

            var fileTarget = new FileTarget
            {
                Name = "FileTarget",
                Layout = "${message}",
                ConcurrentWrites = false,
                FileName = new SimpleLayout(Path.Combine(logDirectory, "current.log")),
                ArchiveEvery = config.ArchivePeriod,
                ArchiveNumbering = ArchiveNumberingMode.Sequence,
                MaxArchiveFiles = config.MaxArchiveFiles,
                ArchiveFileName = new SimpleLayout(Path.Combine(logDirectory,"archive/{####}.log"))
            };
            var asyncWrapper = new AsyncTargetWrapper(fileTarget)
            {
                Name = "AsyncWrapper"
            };

            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.AddTarget(loggerName, asyncWrapper);
            loggingConfiguration.LoggingRules.Add(new LoggingRule("*", LevelToNLogLevel(config.MinimumLogLevel), asyncWrapper));

            LogManager.Configuration = loggingConfiguration;

            return LogManager.GetLogger(loggerName);
        }
    }
}
