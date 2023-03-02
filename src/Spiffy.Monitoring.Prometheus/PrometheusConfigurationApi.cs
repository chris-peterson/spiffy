namespace Spiffy.Monitoring.Prometheus
{
    public class PrometheusConfigurationApi
    {
#if NET6_0_OR_GREATER
        public PrometheusConfigurationApi()
        {
            RuntimeStats = global::Prometheus.DotNetRuntime.DotNetRuntimeStatsBuilder.Customize();
        }

        public global::Prometheus.DotNetRuntime.DotNetRuntimeStatsBuilder.Builder RuntimeStats { get; private set; }
#endif
    }
}
