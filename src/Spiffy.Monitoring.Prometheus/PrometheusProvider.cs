using System;
using Prometheus;
using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring.Prometheus
{
    public static class PrometheusProvider
    {
        public static InitializationApi.ProvidersApi Prometheus(this InitializationApi.ProvidersApi api)
        {
            return Prometheus(api, null);
        }

        public static InitializationApi.ProvidersApi Prometheus(this InitializationApi.ProvidersApi api, Action<PrometheusConfigurationApi> customize)
        {
            var config = new PrometheusConfigurationApi();
            if (customize != null)
            {
                customize(config);
            }
            var metricOptions = new SuppressDefaultMetricOptions
            {
                // these are small and useful; always include
                //   dotnet_*
                //   process_*
                SuppressProcessMetrics = false,

#if NET6_0_OR_GREATER
                // these are small and useful; always include
                //   microsoft_aspnetcore_*
                //   system_runtime_*
                //   system_net_sockets_*
                SuppressEventCounters = false,
#endif

                // these are not useful in most (all?) cases; omit by default
                //   prometheus_net_exemplars_*
                //   prometheus_net_metric_*
                SuppressDebugMetrics = config.SuppressDebugMetrics,

#if NET6_0_OR_GREATER
                // these are insanely large (upwards of 10s of GBs); omit by default
                //   microsoft_aspnetcore_hosting_*
                //   microsoft_aspnetcore_routing_aspnetcore_routing_match_attempts
                //   microsoft_aspnetcore_server_kestrel_kestrel_connection_duration
                //   system_net_http_http_client_*
                SuppressMeters = config.SuppressMeters
#endif
            };
            Metrics.SuppressDefaultMetrics(metricOptions);
            api.Add("prometheus", PrometheusRules.Process);
            return api;
        }
    }
}
