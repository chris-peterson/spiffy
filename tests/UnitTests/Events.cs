using Spiffy.Monitoring;
using Xunit;

namespace UnitTests
{
    public class Events
    {
        [Fact]
        public void Unnamed_event()
        {
            // When:
            var context = new EventContext();
            // Then:
            Assert.Equal(GetType().Name, context.Component);
            Assert.Equal("Unnamed_event", context.Operation);
        }

        [Fact]
        public void Named_event()
        {
            // When:
            var context = new EventContext("MyComponent", "MyOperation");
            // Then:
            Assert.Equal("MyComponent", context.Component);
            Assert.Equal("MyOperation", context.Operation);
        }
    }
}
