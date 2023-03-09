using System;
using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring.Prometheus
{
    public static class PrometheusProvider
    {
        static IDisposable _previousCollector = null;
        static readonly object _serializeCollectionAccess = new object();

        public static InitializationApi.ProvidersApi Prometheus(this InitializationApi.ProvidersApi api)
        {
            return Prometheus(api, null);
        }

        public static InitializationApi.ProvidersApi Prometheus(this InitializationApi.ProvidersApi api, Action<PrometheusConfigurationApi> customize)
        {
            if (customize != null)
            {
                var config = new PrometheusConfigurationApi();
                customize(config);
#if NET6_0_OR_GREATER
                lock (_serializeCollectionAccess)
                {
                    _previousCollector?.Dispose();
                    _previousCollector = config.RuntimeStats.StartCollecting();
                }
#endif
            }
            api.Add("prometheus", PrometheusRules.Process);
            return api;
        }
    }
}
