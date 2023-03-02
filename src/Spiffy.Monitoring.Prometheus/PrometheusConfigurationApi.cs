using Prometheus.DotNetRuntime;

namespace Spiffy.Monitoring.Prometheus
{
    public class PrometheusConfigurationApi
    {
        public PrometheusConfigurationApi()
        {
            RuntimeStats = DotNetRuntimeStatsBuilder.Customize();
        }

        public DotNetRuntimeStatsBuilder.Builder RuntimeStats { get; private set; }
    }
}
