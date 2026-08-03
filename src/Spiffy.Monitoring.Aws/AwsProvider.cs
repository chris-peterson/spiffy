using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Amazon;
using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring.Aws
{
    public static class AwsProvider
    {
        public static InitializationApi.ProvidersApi Aws(this InitializationApi.ProvidersApi providers, Action<AwsConfigurationApi> configure = null)
        {
            var config = new AwsConfigurationApi();
            configure?.Invoke(config);

            AWSConfigs.LoggingConfig.LogTo = LoggingOptions.SystemDiagnostics;
            AWSConfigs.AddTraceListener("Amazon", new AwsEvent(config.SuppressMessages.Prefixes));

            switch (config.LogResponses.ResponseLoggingOption)
            {
                case ResponseLoggingOption.Never:
                    AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.Never;
                    break;
                case ResponseLoggingOption.OnError:
                    AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.OnError;
                    break;
                case ResponseLoggingOption.Always:
                    AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.Always;
                    break;
            }

            return providers;
        }

        internal class AwsEvent : TraceListener
        {
            readonly IReadOnlyList<string> _suppressedPrefixes;

            public AwsEvent(IReadOnlyList<string> suppressedPrefixes)
            {
                _suppressedPrefixes = suppressedPrefixes;
            }

            // TraceData is a System.Diagnostics.TraceListener member, so what's overridden here
            // is BCL surface -- nothing about it is tied to an SDK version, and one
            // implementation serves both majors.  Both report exclusively through
            // TraceSource.TraceData (v3 via InternalSystemDiagnosticsLogger, v4 via
            // DiagnosticAdaptorLogger), which is where the severity is available;
            // Write/WriteLine only ever see the flattened string.  Every v4 message lands on
            // this overload, as do v3's InfoFormat/DebugFormat.
            public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
            {
                Handle(eventType, data?.ToString(), null);
            }

            // Only v3 reaches this overload, from Error/Debug, which pass the message and the
            // exception separately.  v4 accepts an exception and discards it before this point.
            public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
            {
                if (data == null || data.Length == 0)
                {
                    return;
                }
                Handle(eventType, data[0]?.ToString(), data.Length > 1 ? data[1] as Exception : null);
            }

            public override void Write(string message)
            {
                // headers, e.g. Message="Amazon Information: 0 : "
            }

            // Nothing in the SDK reaches the listener this way today, but a caller that
            // bypasses TraceData carries no severity, so treat it as informational.
            public override void WriteLine(string message)
            {
                Handle(TraceEventType.Information, message, null);
            }

            void Handle(TraceEventType eventType, string message, Exception exception)
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }
                if (!IsActionable(eventType) && IsSuppressed(message))
                {
                    return;
                }

                using (var context = new EventContext("AwsSdk", "Event"))
                {
                    context["Message"] = message;

                    if (eventType == TraceEventType.Error || eventType == TraceEventType.Critical)
                    {
                        if (exception == null)
                        {
                            context.SetToError();
                        }
                        else
                        {
                            // also sets the level to error
                            context.IncludeException(exception);
                        }
                    }
                    else
                    {
                        if (eventType == TraceEventType.Warning)
                        {
                            context.SetToWarning();
                        }
                        if (exception != null)
                        {
                            context.IncludeInformationalException(exception, "Exception");
                        }
                    }
                }
            }

            static bool IsActionable(TraceEventType eventType)
            {
                switch (eventType)
                {
                    case TraceEventType.Critical:
                    case TraceEventType.Error:
                    case TraceEventType.Warning:
                        return true;
                    default:
                        return false;
                }
            }

            bool IsSuppressed(string message)
            {
                return _suppressedPrefixes.Any(prefix => message.StartsWith(prefix, StringComparison.Ordinal));
            }

            public override string Name { get; set; } = nameof(AwsEvent);
        }
    }
}
