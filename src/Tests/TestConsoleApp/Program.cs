using System;
using System.Threading;
using Spiffy.Monitoring;

namespace TestConsoleApp
{
    class Program
    {
        static void Main()
        {
            GlobalEventContext.Instance.Set("Application", "TestConsole");

            NLog.Initialize();

            using (var context = new EventContext())
            {
                using (context.Time("WarmUpCache"))
                {
                    Thread.Sleep(1000);
                }

                context.IncludeException(new ApplicationException("foo", new NullReferenceException()));
            }
        }
    }
}
