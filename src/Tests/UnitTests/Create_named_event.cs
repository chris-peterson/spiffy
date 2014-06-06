using FluentAssertions;
using Kekiri;
using Spiffy.Monitoring;

namespace UnitTests
{
    [Scenario]
    public class Create_named_event : ScenarioTest
    {
        private EventContext _context;

        [When]
        public void When_creating_event()
        {
            _context = new EventContext("MyComponent", "MyOperation");
        }

        [Then]
        public void It_should_have_the_correct_component()
        {
            _context.Component.Should().Be("MyComponent");
        }

        [Then]
        public void It_should_have_the_correct_operation()
        {
            _context.Operation.Should().Be("MyOperation");
        }
    }
}
