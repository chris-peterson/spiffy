using FluentAssertions;
using Kekiri;
using Spiffy.Monitoring;

namespace UnitTests
{
    [Scenario]
    public class Create_unnamed_event : ScenarioTest
    {
        private EventContext _context;

        [When]
        public void When_creating_event()
        {
            _context = new EventContext();
        }

        [Then]
        public void It_should_use_the_type_name_for_component()
        {
            _context.Component.Should().Be(GetType().Name);
        }

        [Then]
        public void It_should_use_the_method_name_for_operation()
        {
            _context.Operation.Should().Be("When_creating_event");
        }
    }
}
