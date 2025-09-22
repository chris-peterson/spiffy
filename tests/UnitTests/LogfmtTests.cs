using AwesomeAssertions;
using Kekiri.Xunit;
using Spiffy.Monitoring;

namespace UnitTests
{
    public class LogfmtTests :Scenarios<EventContextTestContext>
    {
        Configuration LogfmtConfig => Configuration.Initialize(c => c.UseLogfmt());
    
        [Scenario]
        public void Basics()
        {
            When(logging);
            Then(it_contains_kvps)
                .And(iso8601_timestamp_is_logged_to_its_own_field);
        }

        [Scenario]
        public void Special_values()
        {
            When(logging_special_values);
            Then(special_values_are_quoted_and_escaped);
        }
        
        void logging()
        {
            Context.Initialize(LogfmtConfig);
            Context.EventContext["foo"] = "bar";
            Context.Log();
        }

        void logging_special_values()
        {
            Context.Initialize(LogfmtConfig);
            Context.EventContext["WithSpaces"] = "value with spaces";
            Context.EventContext["WithEquals"] = "foo=bar";
            Context.EventContext["WithQuotes"] = "\"hello\"";
            Context.Log();
        }

        void it_contains_kvps()
        {
            Context.SingleLogMessage.Should().Contain("foo=bar");
        }
        
        void iso8601_timestamp_is_logged_to_its_own_field()
        {
            Context.SingleLogMessage
                .Should().MatchRegex(@"time=\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3}Z");
        }

        void special_values_are_quoted_and_escaped()
        {
            Context.SingleLogMessage.Should().Contain("WithSpaces=\"value with spaces\"");
            Context.SingleLogMessage.Should().Contain("WithEquals=\"foo=bar\"");
            Context.SingleLogMessage.Should().Contain("WithQuotes=\"\\\"hello\\\"\"");
        }
    }
}
