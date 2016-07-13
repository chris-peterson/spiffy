using System;
using System.Threading;
using NLog.Targets;
using Spiffy.Monitoring;

namespace TestConsoleApp
{
    class Program
    {
        static void Main()
        {
            // this should be the first line of your application
            Spiffy.Monitoring.NLog.Initialize(c => c
                .ArchiveEvery(FileArchivePeriod.Minute)
                .KeepMaxArchiveFiles(5)
                .MinLogLevel(Level.Info)
                .LogToPath(@"Logs"));

            // key-value-pairs set here appear in every event message
            GlobalEventContext.Instance
                .Set("Application", "TestConsole");

            Console.WriteLine("Running application...  Check Logs folder");
            
            // info:
            using (var context = new EventContext("Application", "Start"))
            {
                context["Greeting"] = "Hello world!";
            }

            // warning:
            using (var context = new EventContext("Application", "Run"))
            {
                context.SetToWarning("cause something sorta bad happened");
            }

            // error:
            using (var context = new EventContext("Application", "Crash"))
            {
                context.SetToError("cause something very bad happened");
            }

            var cutOffTime = DateTime.UtcNow.AddMinutes(5);

            while (DateTime.UtcNow < cutOffTime)
            {
                using (var context = new EventContext("Application", "RunTask"))
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
