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
                if (!IsSdkSpam(message))
                {
                    using (var context = new EventContext("AwsSdk", "Event"))
                    {
                        context["Message"] = message;
                        // some example exception messages:
                        // An exception of type HttpErrorResponseException was handled in ErrorHandler...
                        // UnsupportedLanguagePairException making request TranslateTextRequest...
                        // An exception of type TimeoutException was handled in ErrorHandler...
                        if (message.IndexOf("exception", Math.Min(100, message.Length), StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            context.SetToError();
                        }
                    }
                }
            }

            static bool IsSdkSpam(string message)
            {
                // this message happens often and for all configurations (legacy/standard/etc)
                if (message.StartsWith("Resolved DefaultConfigurationMode for RegionEndpoint"))
                {
                    return true;
                }
                // this message happens as part of routine DynamoDB usage
                if (message.StartsWith("Description for table") && message.EndsWith("loaded from SDK Cache"))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
