using System;
using NLog.Targets;

namespace Spiffy.Monitoring
{
    public class NLogTargetsConfigurationApi
    {
        public class FileConfigurationApi
        {
            internal FileArchivePeriod ArchivePeriod { get; private set; } = FileArchivePeriod.Day;
            internal int MaxArchiveFiles { get; private set; } = 2;
            internal string LogDirectory { get; private set; }

            public FileConfigurationApi ArchiveEvery(FileArchivePeriod archivePeriod)
            {
                ArchivePeriod = archivePeriod;
                return this;
            }

            public FileConfigurationApi KeepMaxArchiveFiles(int maxArchiveFiles)
            {
                MaxArchiveFiles = maxArchiveFiles;
                return this;
            }

            public FileConfigurationApi LogToPath(string logDirectory)
            {
                LogDirectory = logDirectory;
                return this;
            }
        }

        internal FileConfigurationApi FileConfiguration { get; private set; }

        public NLogTargetsConfigurationApi File(Action<FileConfigurationApi> customize = null)
        {
            FileConfiguration = new FileConfigurationApi();
            customize?.Invoke(FileConfiguration);
            return this;
        }

        public class ColoredConsoleConfigurationApi
        {
        }

        internal ColoredConsoleConfigurationApi ColoredConsoleConfiguration { get; private set; }

        public NLogTargetsConfigurationApi ColoredConsole(Action<ColoredConsoleConfigurationApi> customize = null)
        {
            ColoredConsoleConfiguration = new ColoredConsoleConfigurationApi();
            customize?.Invoke(ColoredConsoleConfiguration);
            return this;
        }

        public class NetworkConfigurationApi
        {
            public string Address { get; set; } = "udp://127.0.0.1:7071";
        }

        internal NetworkConfigurationApi NetworkConfiguration { get; private set; }

        public NLogTargetsConfigurationApi Network(Action<NetworkConfigurationApi> customize = null)
        {
            NetworkConfiguration = new NetworkConfigurationApi();
            customize?.Invoke(NetworkConfiguration);
            return this;
        }
    }
}