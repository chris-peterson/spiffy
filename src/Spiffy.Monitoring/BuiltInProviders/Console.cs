using Spiffy.Monitoring.Config;

// ReSharper disable once CheckNamespace -- intentional deviation
namespace Spiffy.Monitoring.Console
{
    public static class ConsoleProvider
    {
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

