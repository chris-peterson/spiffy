using System;
using System.Collections.Generic;
using System.Threading;
using Spiffy.Monitoring;
using Spiffy.Monitoring.BuiltIn;
using Spiffy.Monitoring.NLog;
using Spiffy.Monitoring.Prometheus;
using Spiffy.Monitoring.Splunk;

namespace TestConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args?.Length > 0)
            {
                var loggingFlag = args[0].Trim().ToLower();
                switch (loggingFlag)
                {
                    case "trace":
                        Spiffy.Monitoring.Behavior.Initialize(spiffy => {
                            spiffy.Providers.BuiltIn(builtin => builtin
                                .Targets(t => t
                                    .Trace()));
                        });
                        break;
                    case "console":
                        Spiffy.Monitoring.Behavior.Initialize(spiffy => {
                            spiffy.Providers.BuiltIn(builtin => builtin
                                .Targets(t => t
                                    .Console()));
                        });
                        break;
                    case "splunk":
                        Spiffy.Monitoring.Behavior.Initialize(spiffy =>
                        {
                            spiffy.Providers.Splunk(s =>
                            {
                                s.ServerUrl = "http://splunkhec.yourdomain:8088";
                                s.Token = "<secret token>";
                                s.Index = "apps";
                                s.SourceType = "spiffy";
                                s.Source = "testconsoleapp";
                            });
                        });
                        break;
                    case "nlog-file":
                        Spiffy.Monitoring.Behavior.Initialize(spiffy => {
                            spiffy.Providers.NLog(nlog => nlog
                                .Targets(t => t
                                    .File()));
                         });
                        break;
                    case "nlog-coloredconsole":
                        Spiffy.Monitoring.Behavior.Initialize(spiffy => {
                            spiffy.Providers.NLog(nlog => nlog
                                .Targets(t => t
                                    .ColoredConsole()));
                        });
                        break;
                    case "prometheus":
                        Spiffy.Monitoring.Behavior.Initialize(spiffy =>
                        {
                            spiffy.Providers.Prometheus();
                        });
                        PrometheusRules
                            .FromEventContext("myclass", "mymethod")
                                .IncludeLabels("interesting_field")
                                .OverrideValues(OverrideValues)
                            .ToCounter("my_app_my_counter", "Counter Help String");
                        break;
                    case "mix-and-match":
                        Spiffy.Monitoring.Behavior.Initialize(spiffy => {
                            spiffy.Providers.BuiltIn(builtin => builtin.
                                Targets(t => t
                                    .Console()));
                            spiffy.Providers.NLog(nlog => nlog
                                .Targets(t => t
                                    .File()));
                        });
                        break;
                    default:
                        throw new NotSupportedException($"{loggingFlag} did not match any supported value");
                }
            }
            else
            {
                throw new Exception("This test application requires a single string parameter for testing various logging behavior");
            }

            // key-value-pairs set here appear in every event message
            GlobalEventContext.Instance
                .Set("Application", "TestConsole");

            Console.WriteLine("Running application.  Logs are either emitted here, or to 'Logs'");

            // info:
            using (var context = new EventContext("Greetings", "Start"))
            {
                context["Greeting"] = "Hello world!";
            }
            
            while (true)
            {
                // warning:
                using (var context = new EventContext())
                {
                    context.SetToWarning("cause something sorta bad happened");
                }

                // error:
                using (var context = new EventContext())
                {
                    context.SetToError("cause something very bad happened");
                }

                // prometheus counter:
                using (var context = new EventContext("myclass", "mymethod"))
                {
                    context["interesting_field"] = "hello world";
                }

                using (var context = new EventContext())
                {
                    context["MyCustomValue"] = "foo";

                    using (context.Time("LongRunning"))
                    {
                        DoSomethingLongRunning();
                    }

                    try
                    {
                        DoSomethingDangerous();
                    }
                    catch (Exception ex)
                    {
                        context.IncludeException(ex);
                    }
                }
            }
        }

        static void DoSomethingLongRunning()
        {
            Thread.Sleep(1000);
        }

        static void DoSomethingDangerous()
        {
            if (new Random().Next(10) == 0)
            {
                throw new Exception("you were unlucky!", new NullReferenceException());
            }
        }

        static IDictionary<string, string> OverrideValues(LogEvent logEvent)
        {
            return new Dictionary<string, string>
            {
                {"mylabel", "my key"}
            };
        }
    }
}