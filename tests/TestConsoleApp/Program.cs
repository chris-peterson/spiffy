using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog.Targets;
using Spiffy.Monitoring;

namespace TestConsoleApp
{
    class Program
    {
        private static Dictionary<string, ILoggingFacade> loggerCollection = new Dictionary<string, ILoggingFacade>();
        static void Main(string [] args)
        {
            if (args?.Length > 0)
            {
                switch (args[0].Trim().ToLower())
                {
                    case "file":
                        Spiffy.Monitoring.NLog.Initialize(c => c
                            .ArchiveEvery(FileArchivePeriod.Minute)
                            .KeepMaxArchiveFiles(5)
                            .MinLogLevel(Level.Info)
                            .LogToPath(@"Logs"));
                    break;
                    case "trace":
                        DefaultLoggingFacade.Instance.Initialize(LoggingBehavior.Trace);
                    break;
                    case "console":
                        DefaultLoggingFacade.Instance.Initialize(LoggingBehavior.Console);
                    break;
                    case "composite":
                        loggerCollection.Add("console", LoggingFacadeFactory.Create(LoggingBehavior.Console));
                        loggerCollection.Add("trace", LoggingFacadeFactory.Create(LoggingBehavior.Trace));
                        loggerCollection.Add("nlog", NLogFactory.Create(c => c
                            .ArchiveEvery(FileArchivePeriod.Minute)
                            .KeepMaxArchiveFiles(5)
                            .MinLogLevel(Level.Info)
                            .LogToPath(@"Logs")));
                    break;
                }
            }
            else
            {
                // default behavior if nothing is specified (should be console)
            }

            // key-value-pairs set here appear in every event message
            GlobalEventContext.Instance
                .Set("Application", "TestConsole");

            Console.WriteLine("Running application.  Logs are either emitted here, or to 'Logs'");
            
            if(loggerCollection.Any())
            {
                Console.WriteLine("Using CompositeEventContext");
                CompositeContext();
            }
            else
            {
                Console.WriteLine("Using Default EventContext");
                SimpleContext();
            }
        }

        static void SimpleContext() {
            // info:
            using (var context = new EventContext("Greetings", "Start"))
            {
                context["Greeting"] = "Hello world!";
            }

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

            var cutOffTime = DateTime.UtcNow.AddMinutes(5);

            while (DateTime.UtcNow < cutOffTime)
            {
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
        static void CompositeContext() {
            // info:
            using (var context = new CompositeEventContext("Greetings", "Start", loggerCollection))
            {
                context["Greeting"] = "Hello world!";
            }

            // warning:
            using (var context = new CompositeEventContext(loggerCollection))
            {
                context.SetToWarning("cause something sorta bad happened");
            }

            // error:
            using (var context = new CompositeEventContext(loggerCollection))
            {
                context.SetToError("cause something very bad happened");
            }

            var cutOffTime = DateTime.UtcNow.AddMinutes(5);

            while (DateTime.UtcNow < cutOffTime)
            {
                using (var context = new CompositeEventContext(loggerCollection))
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
    }
}
