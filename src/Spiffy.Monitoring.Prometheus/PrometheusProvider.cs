using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring.Prometheus
{
    public static class PrometheusProvider
    {
        public static InitializationApi.ProvidersApi Prometheus(this InitializationApi.ProvidersApi api)
        {
            api.Add("prometheus", PrometheusRules.Process);
            return api;
        }
    }
}
