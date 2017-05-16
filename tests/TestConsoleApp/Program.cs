using System;
using System.Threading;
using Spiffy.Monitoring;

namespace TestConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args?.Length > 0)
            {
                switch (args[0].Trim().ToLower())
                {
                    case "nlog-file":
                        Spiffy.Monitoring.NLog.Initialize(c => c
                            .Targets(t => t
                                .File()));
                        break;
                    case "nlog-coloredconsole":
                        Spiffy.Monitoring.NLog.Initialize(c => c
                            .Targets(t => t
                                .ColoredConsole()));
                        break;
                    case "nlog-all":
                        Spiffy.Monitoring.NLog.Initialize(c => c
                            .Targets(t => t
                                .File()
                                .ColoredConsole()
                                .Network()));
                        break;
                    case "trace":
                        LoggingFacade.Initialize(LoggingBehavior.Trace);
                        break;
                    case "console":
                        LoggingFacade.Initialize(LoggingBehavior.Console);
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
    }
}