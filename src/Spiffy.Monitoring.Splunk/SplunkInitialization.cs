using System;
using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring.Splunk
{
    public class SplunkConfigurationApi
    {
        public string ServerUrl { get; set; }
        public string Token { get; set; }
        public string Index { get; set; }
        public string SourceType { get; set; }
        public string Source { get; set; }
        
        internal SplunkConfigurationApi SplunkConfiguration { get; private set; }
    }

    public static class SplunkInitialization
    {
        static SplunkHttpEventCollector _splunkHttpEventCollector;

        public static InitializationApi.ProvidersApi Splunk(
            this InitializationApi.ProvidersApi providers,
            Action<SplunkConfigurationApi> customize)
        {
            var config = new SplunkConfigurationApi();
            if (customize == null)
            {
                throw new NotSupportedException("Need to configure splunk properties through customization callback");
            }
            customize.Invoke(config);
            var splunk = config.SplunkConfiguration;
            _splunkHttpEventCollector = new SplunkHttpEventCollector
            {
                ServerUrl = splunk.ServerUrl,
                Token = splunk.Token,
                Index = splunk.Index,
                SourceType = splunk.SourceType,
                Source = splunk.Source
            };

            providers.Add("splunk", logEvent => { _splunkHttpEventCollector.Log(logEvent); });
            return providers;
        }
    }
}
