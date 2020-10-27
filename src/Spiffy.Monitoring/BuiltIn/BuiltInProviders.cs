using SD = System.Diagnostics;
using Spiffy.Monitoring.Config;

namespace Spiffy.Monitoring.BuiltIn
{
    public static class BuiltInProviders
    {
        public static InitializationApi.ProvidersApi Trace(this InitializationApi.ProvidersApi providers)
        {
            providers.Add("trace", logEvent =>
            {
                var message = logEvent.Message;
                switch (logEvent.Level)
                {
                    case Level.Info:
                        SD.Trace.TraceInformation(message);
                        break;
                    case Level.Warning:
                        SD.Trace.TraceWarning(message);
                        break;
                    case Level.Error:
                        SD.Trace.TraceError(message);
                        break;
                    default:
                        SD.Trace.WriteLine(message);
                        break;
                }
            });
            return providers;
        }

        public static InitializationApi.ProvidersApi Console(this InitializationApi.ProvidersApi providers)
        {
            providers.Add("console", logEvent =>
            {
                if (logEvent.Level == Level.Error)
                {
                    System.Console.Error.WriteLine(logEvent.MessageWithTime);
                }
                else
                {
                    System.Console.WriteLine(logEvent.MessageWithTime);
                }
            });
            return providers;
        }
    }
}

