using System;

namespace Spiffy.Monitoring
{
    public class NLogConfigurationApi
    {
        internal NLogTargetsConfigurationApi TargetsConfiguration { get; private set; }

        public NLogConfigurationApi Targets(Action<NLogTargetsConfigurationApi> customize)
        {
            TargetsConfiguration = new NLogTargetsConfigurationApi();
            customize(TargetsConfiguration);
            return this;
        }

        internal Level MinimumLogLevel { get; private set; } = Level.Info;

        public NLogConfigurationApi MinLevel(Level minLevel)
        {
            MinimumLogLevel = minLevel;
            return this;
        }
    }
}