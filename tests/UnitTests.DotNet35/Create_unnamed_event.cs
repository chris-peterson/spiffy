using Spiffy.Monitoring;
using NUnit.Framework;

namespace UnitTests.DotNet35
{
    [TestFixture]
    public class Create_unnamed_event
    {
        [Test]
        public void Uses_names_of_caller()
        {
            var context = new EventContext();
            Assert.That(context.Component, Is.EqualTo(GetType().Name));
            Assert.That(context.Operation, Is.EqualTo("Test"));
        }
    }
}
