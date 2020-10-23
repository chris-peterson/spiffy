using System;

namespace Spiffy.Monitoring.BuiltIn
{
    public class BuiltInProvidersConfigurationApi
    {
        internal BuiltInTargetsConfigurationApi TargetsConfiguration { get; private set; }

        public BuiltInProvidersConfigurationApi Targets(Action<BuiltInTargetsConfigurationApi> customize)
        {
            TargetsConfiguration = new BuiltInTargetsConfigurationApi();
            customize?.Invoke(TargetsConfiguration);
            return this;
        }
    }
}
