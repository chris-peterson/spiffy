using System;

namespace Spiffy.Monitoring.BuiltIn
{
    public class BuiltInTargetsConfigurationApi
    {
        internal bool TraceEnabled { get; private set; } = false;
        public BuiltInTargetsConfigurationApi Trace()
        {
            TraceEnabled = true;
            return this;
        }

        internal bool ConsoleEnabled { get; private set; } = false;
        public BuiltInTargetsConfigurationApi Console()
        {
            ConsoleEnabled = true;
            return this;
        }

        internal SplunkConfigurationApi SplunkConfiguration { get; set; }

        public class SplunkConfigurationApi
        {
            public string ServerUrl { get; set; }
            public string Token { get; set; }
            public string Index { get; set; }
            public string SourceType { get; set; }
            public string Source { get; set; }
        }

        public BuiltInTargetsConfigurationApi Splunk(Action<SplunkConfigurationApi> customize = null)
        {
            SplunkConfiguration = new SplunkConfigurationApi();
            customize?.Invoke(SplunkConfiguration);
            return this;
        }
    }
}
