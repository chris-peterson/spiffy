namespace Spiffy.Monitoring.Splunk
{
    public class SplunkConfigurationApi
    {
        public string ServerUrl { get; set; }
        public string Token { get; set; }
        public string Index { get; set; }
        public string SourceType { get; set; }
        public string Source { get; set; }
    }
}