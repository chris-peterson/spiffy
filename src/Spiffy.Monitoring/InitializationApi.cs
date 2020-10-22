using System;

namespace Spiffy.Monitoring
{
    public class InitializationApi
    {
        public class ProvidersApi
        {
            public void AddLoggingAction(string id, Action<LogEvent> loggingAction)
            {
                Behavior.AddLoggingAction(id, loggingAction);
            }
        }

        public ProvidersApi Providers { get; } = new ProvidersApi();
    }
}