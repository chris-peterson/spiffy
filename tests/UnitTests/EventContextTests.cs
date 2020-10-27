using System;
using System.Collections.Generic;
using FluentAssertions;
using Spiffy.Monitoring;
using Kekiri.Xunit;

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
        bool _removeNewlines = false;
        
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
        public void Can_count()
        {
            Given(An_event_context);
            When(Adding_counts);
            Then(The_context_contains_counts);
        }

        [Scenario]
        public void Can_include_exception()
        {
            Given(An_event_context);
            When(Including_an_exception);
            Then(The_context_contains_exception_data, "Exception")
                .And(The_context_level_is, Level.Error);
        }

        [Scenario]
        public void Can_include_informational_exception()
        {
            Given(An_event_context);
            When(Including_an_informational_exception);
            Then(The_context_contains_exception_data, "InfoException")
                .And(The_context_level_is, Level.Info);
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
            Given(Newline_removal_enabled);
            When(Formatting_a_value_with_one_or_more_newline_characters);
            Then(The_formatted_value_has_no_newline_characters);
        }

        private void Newline_removal_enabled()
        {
            _removeNewlines = true;
        }

        private void Formatting_a_value_with_one_or_more_newline_characters()
        {
            using (var context = new EventContext())
            {
                context.AddValues(new KeyValuePair<string, object>("foo", "\nba\tr\r"));

                Behavior.Initialize(customize =>
                {
                    customize.RemoveNewlines = _removeNewlines;
                    customize.Providers.Add("test", logEvent =>
                        Context.FormattedMessage = logEvent.MessageWithTime);
                });
            }
        }

        private void The_formatted_value_has_no_newline_characters()
        {
            var result = (string) Context.FormattedMessage;

            result.Should().NotMatchRegex(
                "[\\r\\n]",
                because: "formatted message should not contain newline characters");
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

        void The_context_contains_counts()
        {
            LogEvent logEvent = null;
            var context = (EventContext) Context.EventContext;

            Behavior.Initialize(customize =>
            {
                customize.RemoveNewlines = _removeNewlines;
                customize.Providers.Add("test", l => logEvent = l);
            });
            context.Dispose();

            logEvent.Message.Contains("foo=1").Should().BeTrue();
            logEvent.Message.Contains("bar=2").Should().BeTrue();
        }
        void Including_an_exception()
        {
           ((EventContext) Context.EventContext).IncludeException(new NullReferenceException());
        }

        void Including_an_informational_exception()
        {
           ((EventContext) Context.EventContext).IncludeInformationalException(new NullReferenceException(), "InfoException");
        }

        void The_context_contains_exception_data(string keyPrefix)
        {
            var context = (EventContext) Context.EventContext;
            context.Contains($"{keyPrefix}_Type").Should().BeTrue();
            context.Contains($"{keyPrefix}_Message").Should().BeTrue();
            context.Contains($"{keyPrefix}_StackTrace").Should().BeTrue();
            context.Contains($"{keyPrefix}").Should().BeTrue();
        }

        void The_context_level_is(Level expectedLevel)
        {
            var context = (EventContext) Context.EventContext;
            context.Level.Should().Be(expectedLevel);
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

        void Adding_counts()
        {
            var context = (EventContext) Context.EventContext;
            context.Count("foo");
            context.Count("bar");
            context.Count("bar");
        }
    }
}
