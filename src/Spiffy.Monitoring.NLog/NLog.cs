using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NLog.Conditions;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Spiffy.Monitoring
{
    public static class NLog
    {
        const string LoggerName = "Spiffy";
        static Logger _logger;

        public static void Initialize(Action<NLogConfigurationApi> configure)
        {
            if (_logger != null) 
                return;

            var config = new NLogConfigurationApi();
            configure(config);

            _logger = SetupNLog(config);

            Behavior.AddCustomLogging(logEvent =>
            {
                var logLevel = logEvent.Level.ToNLogLevel();
                var logEventInfo = new LogEventInfo
                {
                    Level = logLevel,
                    Message = logEvent.Message,
                    TimeStamp = logEvent.Time
                };
                foreach (var kvp in logEvent.Properties)
                {
                    if (!logEventInfo.Properties.ContainsKey(kvp.Key))
                    {
                        logEventInfo.Properties.Add(kvp.Key, kvp.Value);
                    }
                }
                _logger.Log(logEventInfo);
            });
        }

        static Logger SetupNLog(NLogConfigurationApi config)
        {
            var targets = new List<Target>();

            if (config.TargetsConfiguration == null)
            {
                throw new NotSupportedException("Need to configure 'Targets'");
            }

            var file = config.TargetsConfiguration.FileConfiguration;
            if (file != null)
            {
                var logDirectory = string.IsNullOrEmpty(file.LogDirectory) ? "${basedir}/Logs" : file.LogDirectory;

                targets.Add(new FileTarget
                {
                    Name = "FileTarget",
                    Layout = "${message}",
                    ConcurrentWrites = false,
                    FileName = new SimpleLayout(Path.Combine(logDirectory, "current.log")),
                    ArchiveEvery = file.ArchivePeriod,
                    ArchiveNumbering = ArchiveNumberingMode.Sequence,
                    MaxArchiveFiles = file.MaxArchiveFiles,
                    ArchiveFileName = new SimpleLayout(Path.Combine(logDirectory, "archive/{####}.log"))
                });
            }

            var coloredConsole = config.TargetsConfiguration.ColoredConsoleConfiguration;
            if (coloredConsole != null)
            {
                targets.Add(new ColoredConsoleTarget());
            }

            var network = config.TargetsConfiguration.NetworkConfiguration;
            if (network != null)
            {
                targets.Add(new NetworkTarget
                {
                    Address = network.Address
                });
            }

            var splunk = config.TargetsConfiguration.SplunkConfiguration;
            if (splunk != null)
            {
                throw new NotSupportedException();
                // cpeterson TODO:
                // targets.Add(new SplunkHttpEventCollector {
                //     ServerUrl = splunk.ServerUrl,
                //     Token = splunk.Token,
                //     Index = splunk.Index,
                //     SourceType = splunk.SourceType,
                //     Source = splunk.Source,
                //     BatchSizeBytes = 0,
                //     BatchSizeCount = 0
                // });
            }

            if (targets.Count == 0)
            {
                throw new NotSupportedException("Need to specify at least 1 target (e.g. File/ColoredConsole/Network/Splunk)");
            }

            var target = targets.Count == 1 ? targets.Single() : new SplitGroupTarget(targets.ToArray());

            if (config.EnableAsyncLogging)
            {
                target = new AsyncTargetWrapper(target)
                {
                    Name = "AsyncWrapper"
                };
            }

            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.AddTarget(LoggerName, target);
            loggingConfiguration.LoggingRules.Add(new LoggingRule("*", config.MinimumLogLevel.ToNLogLevel(), target));

            LogManager.Configuration = loggingConfiguration;

            return LogManager.GetLogger(LoggerName);
        }
    }
}
