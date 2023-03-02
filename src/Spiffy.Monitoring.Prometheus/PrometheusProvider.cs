using System;
using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring.Prometheus
{
    public static class PrometheusProvider
    {
        static IDisposable _previousCollector = null;

        public static InitializationApi.ProvidersApi Prometheus(this InitializationApi.ProvidersApi api, Action<PrometheusConfigurationApi> customize = null)
        {
            if (customize != null)
            {
                var config = new PrometheusConfigurationApi();
                customize(config);
                _previousCollector?.Dispose();
#if NET6_0_OR_GREATER
                _previousCollector = config.RuntimeStats.StartCollecting();
#endif
            }
            api.Add("prometheus", PrometheusRules.Process);
            return api;
        }
    }
}
