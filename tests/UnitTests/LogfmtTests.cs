using AwesomeAssertions;
using Kekiri.Xunit;
using Spiffy.Monitoring;

namespace UnitTests
{
    public class LogfmtContext
    {
        public LogfmtContext()
        {
            Configuration.Initialize(c =>
            {
                c.UseLogfmt();
            });
            EventContext["foo"] = "bar";
        }

        public EventContext EventContext { get; } = new EventContext();
        public string Message { get; private set; }

        public void Log()
        {
            Message = EventContext.Render().MessageWithTime;
        }
    }

    public class LogfmtTests :Scenarios<LogfmtContext>
    {
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
            Given(special_values);
            When(logging);
            Then(special_values_are_quoted_and_escaped);
        }

        private void special_values()
        {
            Context.EventContext["WithSpaces"] = "value with spaces";
            Context.EventContext["WithEquals"] = "foo=bar";
            Context.EventContext["WithQuotes"] = "\"hello\"";
        }

        void logging()
        {
            Context.Log();
        }

        void it_contains_kvps()
        {
            Context.Message.Should().Contain("foo=bar");
        }
        
        void iso8601_timestamp_is_logged_to_its_own_field()
        {
            Context.Message
                .Should().MatchRegex(@"time=\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3}Z");
        }

        void special_values_are_quoted_and_escaped()
        {
            Context.Message.Should().Contain("WithSpaces=\"value with spaces\"");
            Context.Message.Should().Contain("WithEquals=\"foo=bar\"");
            Context.Message.Should().Contain("WithQuotes=\"\\\"hello\\\"\"");
        }
    }
}
