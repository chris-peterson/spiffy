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

        public static InitializationApi.ProvidersApi Splunk(this InitializationApi.ProvidersApi api,
            Action<SplunkConfigurationApi> customize = null)
        {
            var config = new SplunkConfigurationApi();
            customize?.Invoke(config);
            var splunk = config.SplunkConfiguration;
            _splunkHttpEventCollector = new SplunkHttpEventCollector
            {
                ServerUrl = splunk.ServerUrl,
                Token = splunk.Token,
                Index = splunk.Index,
                SourceType = splunk.SourceType,
                Source = splunk.Source
            };

            api.AddLoggingAction("Splunk", logEvent => { _splunkHttpEventCollector.Log(logEvent); });
            return api;
        }
    }
}
