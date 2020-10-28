using System;
using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring.Splunk
{
    public static class SplunkProvider
    {
        public static InitializationApi.ProvidersApi Splunk(this InitializationApi.ProvidersApi providers, Action<SplunkConfigurationApi> customize)
        {
            var config = new SplunkConfigurationApi();
            if (customize == null)
            {
                throw new NotSupportedException("Need to configure splunk settings");
            }
            customize.Invoke(config);
            _splunkHttpEventCollector = new SplunkHttpEventCollector
            {
                ServerUrl = config.ServerUrl,
                Token = config.Token,
                Index = config.Index,
                SourceType = config.SourceType,
                Source = config.Source
            };

            providers.Add("splunk", logEvent => { _splunkHttpEventCollector.Log(logEvent); });
            return providers;
        }
        
        static SplunkHttpEventCollector _splunkHttpEventCollector;
    }
}
