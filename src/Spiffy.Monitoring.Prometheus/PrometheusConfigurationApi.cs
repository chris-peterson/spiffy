using System;

namespace Spiffy.Monitoring.Prometheus
{
    public class PrometheusConfigurationApi
    {
        internal bool SuppressMeters { get; private set; } = true;
        internal bool SuppressDebugMetrics { get; private set; } = true;

        public PrometheusConfigurationApi()
        {
        }


        [Obsolete("Prometheus.DotNetRuntime is poorly maintained and has been removed from options for this library.  Consider removing, or otherwise move configuration to a different location", true)]
        public object RuntimeStats { get ; } = null;

        public PrometheusConfigurationApi EnableMeters()
        {
            SuppressMeters = false;
            return this;
        }
        
        public PrometheusConfigurationApi EnableDebugMetrics()
        {
            SuppressDebugMetrics = false;
            return this;
        }
    }
}
