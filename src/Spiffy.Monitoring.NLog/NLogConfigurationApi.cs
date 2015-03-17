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
        }

        internal FileArchivePeriod ArchivePeriod { get; private set; }
        internal int MaxArchiveFiles { get; private set; }

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
    }
}