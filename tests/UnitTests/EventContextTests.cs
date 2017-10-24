using System;
using System.Collections.Generic;
using FluentAssertions;
using Spiffy.Monitoring;
using Kekiri.TestRunner.xUnit;

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
            Then(The_context_contains_key_value_pairs);
        }

        [Scenario]
        public void Can_add_multiple_values_via_enumerable()
        {
            Given(An_event_context);
            When(Adding_values_array);
            Then(The_context_contains_key_value_pairs);
        }
        
        [Scenario]
        public void Can_include_exception()
        {
            Given(An_event_context);
            When(Including_an_exception);
            Then(The_context_contains_exception_data);
        }

        [Scenario]
        public void Can_include_structure()
        {
            Given(An_event_context);
            When(Including_a_structure);
            Then(The_context_contains_structure_data);
        }

        [Scenario]
        public void Does_not_remove_newline_characters_by_default()
        {
            Given(An_event_context);
            When(Formatting_a_value_with_one_or_more_newline_characters);
            Then(The_formatted_value_has_newline_characters);
        }

        private void The_formatted_value_has_newline_characters()
        {
            var result = (string) Context.FormattedMessage;

            result.Should().MatchRegex(
                "[\\r\\n]",
                because: "formatted message should not contain newline characters");
        }

        [Scenario]
        public void Can_remove_newline_characters()
        {
            Given(An_event_context_configured_for_newline_removal);
            When(Formatting_a_value_with_one_or_more_newline_characters);
            Then(The_formatted_value_has_no_newline_characters);
        }

        private void An_event_context_configured_for_newline_removal()
        {
            An_event_context();
            Context.NewlineRemovalContext = new NewlineRemovalContext();
        }

        private void Formatting_a_value_with_one_or_more_newline_characters()
        {
            var context = (EventContext) Context.EventContext;

            context.AddValues(new KeyValuePair<string, object>("foo", "\nba\tr\r"));

            Behavior.UseCustomLogging((level, msg) =>
                Context.FormattedMessage = msg);

            context.Dispose();
        }

        private void The_formatted_value_has_no_newline_characters()
        {
            var result = (string) Context.FormattedMessage;

            result.Should().NotMatchRegex(
                "[\\r\\n]",
                because: "formatted message should not contain newline characters");

            using(Context.NewlineRemovalContext) { }
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

        void The_context_contains_key_value_pairs()
        {
            var context = (EventContext) Context.EventContext;
            context.Contains("key1").Should().BeTrue();
            context.Contains("key2").Should().BeTrue();
            context["key1"].Should().Be("value1");
            context["key2"].Should().Be("value2");
        }

        void Including_an_exception()
        {
           ((EventContext) Context.EventContext).IncludeException(new NullReferenceException());
        }

        void The_context_contains_exception_data()
        {
            var context = (EventContext) Context.EventContext;
            context.Contains("Exception_Type").Should().BeTrue();
            context.Contains("Exception_Message").Should().BeTrue();
            context.Contains("Exception_StackTrace").Should().BeTrue();
            context.Contains("Exception").Should().BeTrue();
        }

        class TestStructure
        {
            public int Data1 { get; } = 1;
            public string Data2 { get; } = "foo";
        }

        void Including_a_structure()
        {
            ((EventContext)Context.EventContext).IncludeStructure(new TestStructure(), "Prefix");
        }

        void The_context_contains_structure_data()
        {
            var context = (EventContext)Context.EventContext;
            context.Contains("Prefix_Data1").Should().BeTrue();
            context.Contains("Prefix_Data2").Should().BeTrue();
            context["Prefix_Data1"].Should().Be(1);
            context["Prefix_Data2"].Should().Be("foo");
        }

        private class NewlineRemovalContext : IDisposable
        {
            private readonly bool _oldValue;

            public NewlineRemovalContext()
            {
                _oldValue = Behavior.RemoveNewlines;
                Behavior.RemoveNewlines = true;
            }

            public void Dispose() =>
                Behavior.RemoveNewlines = _oldValue;
        }
    }
}
