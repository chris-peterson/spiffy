using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Spiffy.Monitoring;
using Xunit;

namespace UnitTests
{
    public class ConcurrencyTests
    {
        [Fact]
        public void TestTimers()
        {
            var eventContext = new EventContext();
            var tasks = new List<Task>();
            const int numTasks = 100;
            for (int i = 0; i < numTasks; i++)
            {
                var task = new Task(StaggeredMeasurements, new object [] {i, eventContext});
                task.Start();
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            LogEvent logEvent = null;
            Configuration.Initialize(c => c.Providers.Add(GetType().Name, le => logEvent = le ));
            eventContext.Dispose();

            Assert.InRange(int.Parse(logEvent.Properties["Count_accum"]), numTasks*.65, numTasks*.99);
            Assert.DoesNotContain("Count_once", logEvent.Properties);
            Assert.True(double.Parse(logEvent.Properties["TimeElapsed_accum"]) > double.Parse(logEvent.Properties["TimeElapsed_once"]));
        }

        static void StaggeredMeasurements(object state)
        {
            var param = (object[]) state;
            var delay = (int) param[0];
            var eventContext = (EventContext) param[1];
            using (eventContext.Timers.Accumulate("accum"))
            using (eventContext.Timers.TimeOnce("once"))
            {
                Thread.Sleep(delay);
            }
        }
    }
}
