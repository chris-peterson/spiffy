using System;
using System.Diagnostics;

namespace Spiffy.Monitoring
{
    public static class BuiltInProviderInitialization
    {
        public static void BuiltIn(this InitializationApi.ProvidersApi providers, Action<BuiltInProvidersConfigurationApi> customize)
        {
            var config = new BuiltInProvidersConfigurationApi();
            customize?.Invoke(config);
            if (config.TargetsConfiguration.TraceEnabled)
            {
                Behavior.AddLoggingAction("trace", logEvent =>
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
                Behavior.AddLoggingAction("console", logEvent =>
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

            if (config.TargetsConfiguration.SplunkConfiguration != null)
            {
                Behavior.AddLoggingAction("splunk", logEvent =>
                {
                    // TODO: implement
                    // new SplunkHttpEventCollector {
                    //     //     ServerUrl = splunk.ServerUrl,
                    //     //     Token = splunk.Token,
                    //     //     Index = splunk.Index,
                    //     //     SourceType = splunk.SourceType,
                    //     //     Source = splunk.Source,
                    //     //     BatchSizeBytes = 0,
                    //     //     BatchSizeCount = 0
                    //     // 
                });
            }
        }
    }

    public class BuiltInProvidersConfigurationApi
    {
        internal BuiltInTargetsConfigurationApi TargetsConfiguration { get; private set; }

        public BuiltInProvidersConfigurationApi Targets(Action<BuiltInTargetsConfigurationApi> customize)
        {
            TargetsConfiguration = new BuiltInTargetsConfigurationApi();
            if (customize != null)
            {
                customize(TargetsConfiguration);
            }
            return this;
        }
    }

    public class BuiltInTargetsConfigurationApi
    {
        internal bool TraceEnabled { get; private set; } = false;
        public BuiltInTargetsConfigurationApi Trace()
        {
            TraceEnabled = true;
            return this;
        }

        internal bool ConsoleEnabled { get; private set; } = false;
        public BuiltInTargetsConfigurationApi Console()
        {
            ConsoleEnabled = true;
            return this;
        }

        internal SplunkConfigurationApi SplunkConfiguration { get; set; }

        public class SplunkConfigurationApi
        {
            public string ServerUrl { get; set; }
            public string Token { get; set; }
            public string Index { get; set; }
            public string SourceType { get; set; }
            public string Source { get; set; }
        }

        public BuiltInTargetsConfigurationApi Splunk(Action<SplunkConfigurationApi> customize = null)
        {
            SplunkConfiguration = new SplunkConfigurationApi();
            customize?.Invoke(SplunkConfiguration);
            return this;
        }
    }
}