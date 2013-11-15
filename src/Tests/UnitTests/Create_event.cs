using FluentAssertions;
using Kekiri;
using Spiffy;

namespace UnitTests
{
    [Scenario]
    public class Create_event : ScenarioTest
    {
        private Monitor _monitor;
        private Event _event;

        [Given]
        public void Given_a_monitor()
        {
            _monitor = new Monitor();
        }

        [When]
        public void When_creating_an_event()
        {
            _event = _monitor.CreateEvent();
        }

        [Then]
        public void It_should_have_basic_values()
        {
            _event.Component.Should().Be(GetType().Name);
            _event.Operation.Should().Be("When_creating_an_event");
        }

        [Then]
        public void And_it_should_be_customizable()
        {
            _event["key"] = "value";
            _event["key"].Should().Be("value");
        }
    }
}
