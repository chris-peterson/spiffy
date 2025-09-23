using System.Threading;
using System.Threading.Tasks;
using Kekiri.Xunit;
using Xunit;

namespace UnitTests;

public class TimerCollectionTests : Scenarios<EventContextTestContext>
{
    protected override Task BeforeAsync()
    {
        Context.Initialize();
        return base.BeforeAsync();
    }

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
        using (Context.EventContext.Timers.TimeOnce(TimerKey))
        {
            Thread.Sleep(50);
        }
    }

    void A_code_block_timed_multiple_times()
    {
        for (int i = 0; i < 2; i++)
        {
            using (Context.EventContext.Timers.Accumulate(TimerKey))
            {
                Thread.Sleep(50);
            }
        }
    }
        
    void Event_is_logged()
    {
        Context.Log();
    }

    void The_time_elapsed_field_should_be_near(int target)
    {
        Assert.InRange(double.Parse(Context.SingleLogEvent.Properties[$"TimeElapsed_{TimerKey}"]), target-20, target+20);
    }

    void There_should_be_no_count_field()
    {
        Assert.DoesNotContain($"Count_{TimerKey}", Context.SingleLogEvent.Properties);
    }

    void There_should_be_a_count_field()
    {
        var keyName = $"Count_{TimerKey}";
        Assert.Contains(keyName, Context.SingleLogEvent.Properties);
        Assert.Equal(2, int.Parse(Context.SingleLogEvent.Properties[keyName]));
    }
    const string TimerKey = "abc";
}
