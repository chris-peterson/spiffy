using System;
using System.Diagnostics;
using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring.BuiltIn
{
    public static class BuiltInProviders
    {
        public static void BuiltIn(this InitializationApi.ProvidersApi providers, Action<BuiltInProvidersConfigurationApi> customize)
        {
            var config = new BuiltInProvidersConfigurationApi();
            customize?.Invoke(config);
            if (config.TargetsConfiguration.TraceEnabled)
            {
                providers.AddLoggingAction("trace", logEvent =>
                {
                    var message = logEvent.Message;
                    switch (logEvent.Level)
                    {
                        case Level.Info:
                            Trace.TraceInformation(message);
                            break;
                        case Level.Warning:
                            Trace.TraceWarning(message);
                            break;
                        case Level.Error:
                            Trace.TraceError(message);
                            break;
                        default:
                            Trace.WriteLine(message);
                            break;
                    }
                });
            }
            if (config.TargetsConfiguration.ConsoleEnabled)
            {
                providers.AddLoggingAction("console", logEvent =>
                {
                    if (logEvent.Level == Level.Error)
                    {
                        Console.Error.WriteLine(logEvent.MessageWithTime);
                    }
                    else
                    {
                        Console.WriteLine(logEvent.MessageWithTime);
                    }
                });
            }
        }
    }
}
