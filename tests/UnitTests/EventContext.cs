using System.Collections.Generic;
using FluentAssertions;
using Spiffy.Monitoring;
using Kekiri.TestRunner.xUnit;
using Xunit;

namespace UnitTests
{
    public class EventContextCreation : Scenarios
    {
        [Scenario]
        public void Implicit_creation()
        {
            When(Creating_an_event_context, new EventContext());
            Then(Component_and_operation_should_be, GetType().Name, "Implicit_creation");
        }


        [Scenario]
        public void Explicit_creation()
        {
            When(Creating_an_event_context, new EventContext("MyComponent", "MyOperation"));
            Then(Component_and_operation_should_be, "MyComponent", "MyOperation");
        }

        void Component_and_operation_should_be(string component, string operation)
        {
            var context = (EventContext) Context.EventContext;
            context.Component.Should().Be(component);
            context.Operation.Should().Be(operation);
        }

        void Creating_an_event_context(EventContext eventContext)
        {
            Context.EventContext = eventContext;
        }
    }

    public class EventContextValues : Scenarios
    {
        [Scenario]
        public void Can_add_multiple_values_via_params()
        {
            Given(An_event_context);
            When(Adding_values_via_params);
            Then(The_context_contains_the_expected_key);
        }

        [Scenario]
        public void Can_add_multiple_values_via_enumerable()
        {
            Given(An_event_context);
            When(Adding_values_array);
            Then(The_context_contains_the_expected_key);
        }

        void An_event_context()
        {
            Context.EventContext = new EventContext();
        }

        void Adding_values_via_params()
        {
            Context.EventContext.AddValues(
                new KeyValuePair<string, object>("key1", "value1"),
                new KeyValuePair<string, object>("key2", "value2"));
        }

        void Adding_values_array()
        {
            Context.EventContext.AddValues(
                new List<KeyValuePair<string, object>>
                {
                    new KeyValuePair<string, object>("key1", "value1"),
                    new KeyValuePair<string, object>("key2", "value2")
                });
        }

        void The_context_contains_the_expected_key()
        {
            var context = (EventContext) Context.EventContext;
            context.Contains("key1").Should().BeTrue();
            context.Contains("key2").Should().BeTrue();
            context["key1"].Should().Be("value1");
            context["key2"].Should().Be("value2");
        }
    }
}
