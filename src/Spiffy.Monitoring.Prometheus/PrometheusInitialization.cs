using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring.Prometheus
{
    public static class PrometheusInitialization
    {
        public static InitializationApi.ProvidersApi Prometheus(this InitializationApi.ProvidersApi api)
        {
            api.Add("Prometheus", PrometheusRules.Process);
            return api;
        }
    }
}
