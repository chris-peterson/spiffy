using Spiffy.Monitoring;
using Xunit;

namespace UnitTests
{
    public class Events
    {
        EventContext _context;

        [Fact]
        public void Unnamed_event()
        {
            // When:
            _context = new EventContext();
            // Then:
            It_should_use_the_type_name_for_component();
            It_should_use_the_method_name_for_operation();
        }

        [Fact]
        public void Named_event()
        {
            // When:
            _context = new EventContext("MyComponent", "MyOperation");
            // Then:
            Assert.Equal("MyComponent", _context.Component);
            Assert.Equal("MyOperation", _context.Operation);
        }

        void It_should_use_the_type_name_for_component()
        {
            Assert.Equal(GetType().Name, _context.Component);
        }

        void It_should_use_the_method_name_for_operation()
        {
            Assert.Equal("Creating_event", _context.Operation);
        }
    }
}
