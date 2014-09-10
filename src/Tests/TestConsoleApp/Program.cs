using System;
using System.Threading;
using Spiffy.Monitoring;

namespace TestConsoleApp
{
    class Program
    {
        static void Main()
        {
            // this should be the first line of your application
            NLog.Initialize();

            // key-value-pairs set here appear in every event message
            GlobalEventContext.Instance
                .Set("Application", "TestConsole");

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

        static void DoSomethingLongRunning()
        {
            Thread.Sleep(1000);
        }

        static void DoSomethingDangerous()
        {
            if (new Random().Next(100) == 0)
            {
                throw new ApplicationException("you were unlucky!", new NullReferenceException());
            }
        }
    }
}
