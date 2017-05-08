using System.Collections.Generic;
using Spiffy.Monitoring;
using Xunit;

namespace UnitTests
{
    public class CompositeEvents
    {
        [Fact]
        public void Unnamed_event()
        {
            // When:
            var context = new CompositeEventContext();
            // Then:
            Assert.Equal(GetType().Name, context.Component);
            Assert.Equal("Unnamed_event", context.Operation);
        }

        [Fact]
        public void Named_event()
        {
            // When:
            var context = new CompositeEventContext("MyComponent", "MyOperation", new Dictionary<string, ILoggingFacade> { {"default", LoggingFacade.Instance} });
            // Then:
            Assert.Equal("MyComponent", context.Component);
            Assert.Equal("MyOperation", context.Operation);
        }
    }
}
