using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Spiffy.Monitoring.Splunk
{
    public class SplunkHttpEventCollector
    {
        private readonly HttpClient _client = new HttpClient();

        public string Index { get; set; }
        public string SourceType { get; set; }
        public string ServerUrl { get; set; }
        public string Token { get; set; }
        public string Source { get; set; }

        public void Log(LogEvent logEvent)
        {
            var splunkEvent = new SplunkEvent
            {
                Time = new DateTimeOffset(logEvent.Timestamp).ToUnixTimeSeconds(),
                Index = Index,
                Source = Source,
                SourceType = SourceType,
                Host = "lambda",
                Event = logEvent.Message
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/services/collector/event")
            {
                Content = JsonContent.Create(splunkEvent)
            };
            request.Headers.Add("User-Agent", "Spiffy.Monitoring SplunkHttpEventCollector");
            request.Headers.Authorization = new AuthenticationHeaderValue("Splunk", Token);

            _client.SendAsync(request).GetAwaiter().GetResult();
        }

        class SplunkEvent
        {
            [JsonPropertyName("time")] public long Time { get; set; }

            [JsonPropertyName("source")] public string Source { get; set; }

            [JsonPropertyName("sourcetype")] public string SourceType { get; set; }

            [JsonPropertyName("index")] public string Index { get; set; }

            [JsonPropertyName("host")] public string Host { get; set; }

            [JsonPropertyName("event")] public string Event { get; set; }
        }
    }
}