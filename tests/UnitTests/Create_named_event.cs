using FluentAssertions;
using Kekiri.TestRunner.NUnit;
using Spiffy.Monitoring;

namespace UnitTests
{
    public class Create_named_event : Scenario
    {
        EventContext _context;

        public Create_named_event()
        {
            When(Creating_event);
            Then(It_should_have_the_correct_component)
              .And(It_should_have_the_correct_operation);
        }

        void Creating_event()
        {
            _context = new EventContext("MyComponent", "MyOperation");
        }

        void It_should_have_the_correct_component()
        {
            _context.Component.Should().Be("MyComponent");
        }

        void It_should_have_the_correct_operation()
        {
            _context.Operation.Should().Be("MyOperation");
        }
    }
}
