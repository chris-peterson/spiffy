using System;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using Kekiri;
using Kekiri.Xunit;
using Spiffy.Monitoring;

namespace UnitTests
{
    public class Publishing : Scenarios
    {
        PublishingTestContext _context = new PublishingTestContext();

        [Scenario]
        public void Events_are_published_on_disposal()
        {
            Given(A_publishing_context);
            When(Disposing_an_event_context);
            Then(It_should_publish_the_log_message);
        }

        [Scenario]
        public void Suppressed_events_are_not_published()
        {
            Given(A_publishing_context)
            .And(EventContext_is_suppressed);
            When(Disposing_an_event_context);
            Then(It_should_not_publish_the_log_message);
        }

        [Scenario]
        public void Events_are_only_published_once()
        {
            Given(A_publishing_context)
                .And(Disposing_an_event_context);
            When(Disposing_an_event_context);
            Then(It_should_publish_the_log_message)
                .But(It_should_not_publish_again);
        }

        [Scenario]
        public void Values_with_spaces_are_encapsulated_in_quotes()
        {
            string str = "value with spaces";
            Given(A_publishing_context)
                .And(Context_contains_value, str);
            When(Disposing_an_event_context);
            Then(It_should_publish_the_log_message)
                .And(The_value_should_be_encapsulated_in_quotes, str);
        }

        [Scenario]
        public void Values_with_commas_are_encapsulated_in_quotes()
        {
            string str = "value1,value2";
            Given(A_publishing_context)
                .And(Context_contains_value, str);
            When(Disposing_an_event_context);
            Then(It_should_publish_the_log_message)
                .And(The_value_should_be_encapsulated_in_quotes, str);
        }

        [Scenario]
        public void Single_timing()
        {
            Given(A_publishing_context)
                .And(A_code_block_is_timed);
            When(Disposing_an_event_context);
            Then(It_should_publish_the_log_message)
                .And(The_message_contains_TimeElapsed)
                .But(The_nessage_contains_Count, false);
        }

        [Scenario]
        public void Multiple_timings()
        {
            Given(A_publishing_context)
                .And(A_code_block_is_timed)
                .And(The_same_block_is_timed_again);
            When(Disposing_an_event_context);
            Then(It_should_publish_the_log_message)
                .And(The_message_contains_TimeElapsed)
                .But(The_nessage_contains_Count, true);
        }

        void A_code_block_is_timed()
        {
            using (_context.EventContext.Time("TimingKey"))
            {
            }
        }

        void The_same_block_is_timed_again()
        {
            A_code_block_is_timed();
        }

        void The_message_contains_TimeElapsed()
        {
            _context.Messages.Single().Item2.Contains("TimingKey");
        }

        void The_nessage_contains_Count(bool shouldContain)
        {
            const string CountKey = "Count_TimingKey";
            var message = _context.Messages.Single().Item2;
            if (shouldContain)
            {
                message.Should().Contain(CountKey);
            }
            else
            {
                message.Should().NotContain(CountKey);
            }
        }

        void A_publishing_context()
        {
            _context =new PublishingTestContext();
        }

        void EventContext_is_suppressed()
        {
            _context.EventContext.Suppress();
        }

        void Disposing_an_event_context()
        {
            _context.EventContext.Dispose();
        }

        void It_should_publish_the_log_message()
        {
            var message = _context.Messages.Single();
            message.Item1.Should().Be(Level.Info);
            message.Item2.Length.Should().BeInRange(10, 200);
        }

        void It_should_not_publish_again()
        {
            _context.Messages.Count.Should().Be(1);
        }

        void It_should_not_publish_the_log_message()
        {
            _context.Messages.Count.Should().Be(0);
        }

        void Context_contains_value(string value)
        {
            _context.EventContext["Key"] = value;
        }

        void The_value_should_be_encapsulated_in_quotes(string value)
        {
            _context.Messages.Single().Item2.Should()
                .Contain($"\"{value}\"");
        }

        class PublishingTestContext
        {
            public PublishingTestContext()
            {
                Behavior.UseCustomLogging((level, msg) =>
                    Messages.Add(new Tuple<Level, string>(level, msg)));
            }

            public EventContext EventContext { get; } = new EventContext("MyComponent", "MyOperation");
            public List<Tuple<Level, string>> Messages { get; } = new List<Tuple<Level, string>>();
        }
    }
}
