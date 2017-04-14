using System;
using System.Linq;
using System.Collections.Generic;
using Spiffy.Monitoring;
using Xunit;

namespace UnitTests
{
    public class Publishing
    {
        readonly PublishingTestContext _context = new PublishingTestContext();

        [Fact]
        public void Events_are_published()
        {
            // When:
            Disposing_an_event_context();
            // Then:
            It_should_publish_the_log_message();
        }
    
        [Fact]
        public void Duplicate_disposal_does_not_double_publish()
        {
            // Given:
            Event_has_already_been_disposed();
            // When:
            Disposing_an_event_context();
            // Then:
            It_should_not_publish_again();
        }


        void Event_has_already_been_disposed()
        {
            _context.EventContext.Dispose();
        }

        void Disposing_an_event_context()
        {
            _context.EventContext.Dispose();
        }

        void It_should_publish_the_log_message()
        {
            var message = _context.Messages.Single();
            Assert.Equal(Level.Info, message.Item1);
            Assert.InRange(message.Item2.Length, 10, 100);
        }

        void It_should_not_publish_again()
        {
            Assert.Equal(1, _context.Messages.Count);
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
