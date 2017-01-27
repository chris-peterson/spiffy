using Kekiri.TestRunner.NUnit;
using NUnit.Framework;
using Spiffy.Monitoring;

namespace UnitTests
{
    public class Create_unnamed_event : Scenario
    {
        EventContext _context;

        public Create_unnamed_event()
        {
            When(Creating_event);
            Then(It_should_use_the_type_name_for_component)
              .And(It_should_use_the_method_name_for_operation);
        }

        void Creating_event()
        {
            _context = new EventContext();
        }

        void It_should_use_the_type_name_for_component()
        {
            Assert.That(_context.Component, Is.EqualTo(GetType().Name));
        }

        void It_should_use_the_method_name_for_operation()
        {
            Assert.That(_context.Operation, Is.EqualTo("Creating_event"));
        }
    }
}
