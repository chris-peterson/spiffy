using System;
using System.Diagnostics;
using Amazon;
using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring.Aws
{
    public static class AwsProvider
    {
        public static InitializationApi.ProvidersApi Aws(this InitializationApi.ProvidersApi providers, Action<AwsConfigurationApi> configure = null)
        {
            AWSConfigs.LoggingConfig.LogTo = LoggingOptions.SystemDiagnostics;
            AWSConfigs.AddTraceListener("Amazon", new AwsEvent());
            AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.OnError;

            if (configure != null)
            {
                var config = new AwsConfigurationApi();
                configure(config);
            }

            return providers;
        }

        class AwsEvent : TraceListener
        {
            public override void Write(string message)
            {
                // these messages tend to be useless, e.g.
                // Message="Amazon Information: 0 : "
            }

            public override void WriteLine(string message)
            {
                Handle(message);
            }

            void Handle(string message)
            {
                using (var context = new EventContext())
                {
                    context.Operation = "Unknown";
                    context["Message"] = message;
                }
            }
        }
    }
}
