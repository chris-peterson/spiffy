using System;
using NLog.Targets;

namespace Spiffy.Monitoring
{
    public class NLogConfigurationApi
    {
        public NLogConfigurationApi()
        {
            ArchivePeriod = FileArchivePeriod.Day;
            MaxArchiveFiles = 2;
            MinimumLogLevel = Level.Info;
        }

        internal FileArchivePeriod ArchivePeriod { get; private set; }
        internal int MaxArchiveFiles { get; private set; }
        internal Level MinimumLogLevel { get; private set; }
        internal string LogDirectory { get; private set; }

        public NLogConfigurationApi ArchiveEvery(FileArchivePeriod archivePeriod)
        {
            ArchivePeriod = archivePeriod;
            return this;
        }

        public NLogConfigurationApi KeepMaxArchiveFiles(int maxArchiveFiles)
        {
            MaxArchiveFiles = maxArchiveFiles;
            return this;
        }

        public NLogConfigurationApi ChangeLogDirectory(string logDirectory)
        {
            LogDirectory = logDirectory;
            return this;
        }

        /// <summary>
        /// Log at the minLogLevel and below
        /// </summary>
        /// <param name="minLogLevel"></param>
        /// <returns></returns>
        public NLogConfigurationApi MinLogLevel(Level minLogLevel)
        {
            MinimumLogLevel = minLogLevel;
            return this;
        }
    }
}