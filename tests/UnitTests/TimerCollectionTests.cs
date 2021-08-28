using System.Threading;
using Spiffy.Monitoring;
using Kekiri.Xunit;
using Xunit;

namespace UnitTests
{
    public class TimerCollectionTests : Scenarios
    {
        [Scenario]
        public void TimeOnceScenario()
        {
            Given(A_code_block_timed_once);
            When(Event_is_logged);
            Then(The_time_elapsed_field_should_be_near, 50)
                .And(There_should_be_no_count_field);
        }

        [Scenario]
        public void AccumulateScenario()
        {
            Given(A_code_block_timed_multiple_times);
            When(Event_is_logged);
            Then(The_time_elapsed_field_should_be_near, 100)
                .And(There_should_be_a_count_field);
        }

        void A_code_block_timed_once()
        {
            using (EventContext.Timers.TimeOnce(TimerKey))
            {
                Thread.Sleep(50);
            }
        }

        void A_code_block_timed_multiple_times()
        {
            for (int i = 0; i < 2; i++)
            {
                using (EventContext.Timers.Accumulate(TimerKey))
                {
                    Thread.Sleep(50);
                }
            }
        }
        
        void Event_is_logged()
        {
            Configuration.Initialize(c => c.Providers.Add(GetType().Name, logEvent => LoggedEvent = logEvent));
            EventContext.Dispose();
        }

        void The_time_elapsed_field_should_be_near(int target)
        {
            Assert.InRange(double.Parse(LoggedEvent.Properties[$"TimeElapsed_{TimerKey}"]), target, target+10);
        }

        void There_should_be_no_count_field()
        {
            Assert.DoesNotContain($"Count_{TimerKey}", LoggedEvent.Properties);
        }

        void There_should_be_a_count_field()
        {
            var keyName = $"Count_{TimerKey}";
            Assert.Contains(keyName, LoggedEvent.Properties);
            Assert.Equal(2, int.Parse(LoggedEvent.Properties[keyName]));
        }

        EventContext EventContext { get; } = new EventContext();
        LogEvent LoggedEvent { get; set; }
        const string TimerKey = "abc";
    }
}
