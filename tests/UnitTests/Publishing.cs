using System;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using Kekiri;
using Kekiri.TestRunner.xUnit;
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
            message.Item2.Length.Should().BeInRange(10, 100);
        }

        void It_should_not_publish_again()
        {
            _context.Messages.Count.Should().Be(1);
        }

        void It_should_not_publish_the_log_message()
        {
            _context.Messages.Count.Should().Be(0);
        }

        class PublishingTestContext
        {
            public PublishingTestContext()
            {
                LoggingFacade.Initialize((level, msg) =>
                    Messages.Add(new Tuple<Level, string>(level, msg)));
            }

            public EventContext EventContext { get; } = new EventContext("MyComponent", "MyOperation");
            public List<Tuple<Level, string>> Messages { get; } = new List<Tuple<Level, string>>();
        }
    }
}
