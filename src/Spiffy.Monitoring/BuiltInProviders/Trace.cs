using Spiffy.Monitoring.Config;
using SD = System.Diagnostics;

// ReSharper disable once CheckNamespace -- intentional deviation
namespace Spiffy.Monitoring.Trace
{
    public static class TraceProvider
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
    }
}

