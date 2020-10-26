using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring.Prometheus
{
    public static class PrometheusInitialization
    {
        public static void Prometheus(this InitializationApi.ProvidersApi api)
        {
            api.AddLoggingAction("Prometheus", PrometheusRules.Process);
        }
    }
}
