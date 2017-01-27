using System;
using System.Linq;
using System.Collections.Generic;
using Kekiri.TestRunner.NUnit;
using NUnit.Framework;
using Spiffy.Monitoring;

namespace UnitTests
{
    public class Events_are_published : Scenario<PublishingTestContext>
    {
        public Events_are_published()
        {
            When(Disposing_an_event_context);
            Then(It_should_publish_the_log_message);
        }

        void Disposing_an_event_context()
        {
            Context.EventContext.Dispose();
        }

        void It_should_publish_the_log_message()
        {
            var message = Context.Messages.Single();
            Assert.That(message.Item1, Is.EqualTo(Level.Info));
            Assert.That(message.Item2.Length, Is.GreaterThan(10));
        }
    }

    public class Events_are_published_once : Scenario<PublishingTestContext>
    {
        public Events_are_published_once()
        {
            Given(Event_has_already_been_disposed);
            When(Disposing_an_event_context);
            Then(It_should_not_publish_again);
        }

        void Event_has_already_been_disposed()
        {
            Context.EventContext.Dispose();
        }

        void Disposing_an_event_context()
        {
            Context.EventContext.Dispose();
        }

        void It_should_not_publish_again()
        {
            Assert.That(Context.Messages.Count, Is.EqualTo(1));
        }
    }
    public class PublishingTestContext
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
