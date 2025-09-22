using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Spiffy.Monitoring;
using Kekiri.Xunit;

namespace UnitTests;

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

    [Scenario]
    public void Implicit_creation_from_async_context()
    {
        WhenAsync(Creating_an_event_context_async);
        Then(Component_and_operation_should_be, GetType().Name, nameof(Creating_an_event_context_async));
    }

    [Scenario]
    public void Created_via_reflection()
    {
        When(Creating_an_event_context_via_reflection);
        Then(Component_and_operation_should_be, "RuntimeType", "CreateInstanceDefaultCtor");
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

    async Task Creating_an_event_context_async()
    {
        Context.EventContext = new EventContext();
        await Task.CompletedTask;
    }

    void Creating_an_event_context_via_reflection()
    {
        Context.EventContext = Activator.CreateInstance(typeof(EventContext));
    }
}

public class EventContextTestContext
{
    readonly List<LogEvent> _loggedEvents = new();
    public void Initialize(Configuration config = null)
    {
        config ??= Configuration.Create(c =>
            {
                c.Providers.Add("test", le => _loggedEvents.Add(le));
            });
        config.LoggingActions = [ _loggedEvents.Add ];
        EventContext = new EventContext("TestComponent", "TestOperation", config);
    }

    public EventContext EventContext { get; private set; }

    public void Log()
    {
        EventContext.Dispose();
    }

    public LogEvent SingleLogEvent => _loggedEvents.Single();
    public string SingleLogMessage => SingleLogEvent.MessageWithTime;
}

public class EventContextValues : Scenarios<EventContextTestContext>
{
    bool _removeNewlines = false;

    [Scenario]
    public void TimeElapsed_works()
    {
        Given(An_event_context);
        When(Measuring_something_slow);
        Then(The_published_log_message_has_expected_TimeElapsed);
    }

    void Measuring_something_slow()
    {
        Thread.Sleep(100);
        Context.Log();
    }

    void The_published_log_message_has_expected_TimeElapsed()
    {
        var te = Context.SingleLogEvent.TimeElapsed;
        te.Should().BeGreaterThan(TimeSpan.FromMilliseconds(50));
        var kvps = (string[]) Context.SingleLogMessage.Split(' ');
        var timeElapsed = kvps.Single(k => k.StartsWith("TimeElapsed="));
        var timeElapsedSplit = timeElapsed.Split('=');
        var timeElapsedValue = TimeSpan.FromMilliseconds(float.Parse(timeElapsedSplit[1]));
        timeElapsedValue.Should().BeCloseTo(te, TimeSpan.FromMilliseconds(1));
    }

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

    [ScenarioOutline]
    [Example("foo", "foo", "no encapsulation needed")]
    [Example("a b", "\"a b\"", "spaces are encapsulated")]
    [Example("a,b", "\"a,b\"", "commas are encapsulated")]
    [Example("a=b", "\"a=b\"", "equals are encapsulated")]
    [Example("a&b", "\"a&b\"", "ampersands are encapsulated")]
    [Example("\"", "'\"'", "double quotes are wrapped in single quotes")]
    [Example("\"'", "`\"'", "if double and single quotes are used, wrap in backtick")]
    [Example("\"'`", "\"\"'`", "if all quote types are used, use double quotes")]
    [Example("\"'`foo", "\"\"'`foo", "don't throw out-of-range-exceptions (regression test for !35)")]
    public void Values_are_encapsulated_with_quotes_if_necessary(string input, string expectedResult, string reason)
    {
        Given(An_event_context);
        When(Formatting_value, input);
        Then(It_should_be, expectedResult, reason);
    }

    private void The_formatted_value_has_newline_characters()
    {
        Context.SingleLogMessage.Should().MatchRegex(
            "[\\r\\n]",
            because: "formatted message should contain newline characters");
    }

    private void It_should_be(string expectedResult, string reasons)
    {
        Context.SingleLogMessage.Should().Contain(expectedResult, because: reasons);
    }

    [Scenario]
    public void Logs_with_short_values_are_slotted_in_order()
    {
        Given(An_event_context);
        When(An_event_is_comprised_of_short_values);
        Then(The_values_are_in_the_order_assigned);
    }

    [ScenarioOutline]
    [Example("foo", "bar", "foo,bar")]
    [Example("foo", null, "foo,")]
    [Example(null, "foo", ",foo")]
    [Example(null, null, ",")]
    public void Can_append_to_value(string value1, string value2, string expectedOutput)
    {
        Given(An_event_context);
        When(Appending_values, value1, value2);
        Then(The_value_is_present, expectedOutput);

        const string Key = "key";

        void Appending_values(string str1, string str2)
        {
            Context.EventContext.AppendToValue(Key, str1, ",");
            Context.EventContext.AppendToValue(Key, str2, ",");
        }

        void The_value_is_present(string expectedOutput)
        {
            ((string)Context.EventContext[Key].ToString()).Should().Be(expectedOutput);
        }
    }

    private void The_values_are_in_the_order_assigned()
    {
        var indexOfKey1 = Context.SingleLogMessage.IndexOf(" Key1=", StringComparison.InvariantCultureIgnoreCase);
        var indexOfKey2 = Context.SingleLogMessage.IndexOf(" Key2=", StringComparison.InvariantCultureIgnoreCase);

        indexOfKey1.Should().BeLessThan(indexOfKey2);
    }

    [Scenario]
    public void Long_values_are_deprioritized_in_log_messages()
    {
        Given(An_event_context);
        When(An_event_has_a_mix_of_short_and_long_values);
        Then(The_long_values_are_deprioritized);
    }

    private void The_long_values_are_deprioritized()
    {
        var indexOfKey1 = Context.SingleLogMessage.IndexOf(" Key1=", StringComparison.InvariantCultureIgnoreCase);
        var indexOfKey2 = Context.SingleLogMessage.IndexOf(" Key2=", StringComparison.InvariantCultureIgnoreCase);
        var indexOfKey3 = Context.SingleLogMessage.IndexOf(" Key3=", StringComparison.InvariantCultureIgnoreCase);

        indexOfKey1.Should().BeLessThan(indexOfKey2);
        indexOfKey3.Should().BeLessThan(indexOfKey2);
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
        Context.Initialize(Configuration.Create(c => c.RemoveNewlines = _removeNewlines));
        Context.EventContext.AddValues(new KeyValuePair<string, object>("foo", "\nba\tr\r"));
        Context.Log();
    }

    private void Formatting_value(string input)
    {
        Context.Initialize();
        Context.EventContext["Key"] = input;
        Context.Log();
    }

    private void The_formatted_value_has_no_newline_characters()
    {
        Context.SingleLogMessage.Should().NotMatchRegex(
            "[\\r\\n]",
            because: "formatted message should not contain newline characters");
    }

    void An_event_context()
    {
        Context.Initialize();
    }

    private void An_event_is_comprised_of_short_values()
    {
        Context.Initialize(Configuration.Create(c => c.DeprioritizedValueLength = 30));
        Context.EventContext["Key1"] = "A short message";
        Context.EventContext["Key2"] = "Another short message";
        Context.Log();
    }

    private void An_event_has_a_mix_of_short_and_long_values()
    {
        Context.Initialize(Configuration.Create(c => c.DeprioritizedValueLength = 30));
        Context.EventContext["Key1"] = "A short message";
        Context.EventContext["Key2"] = "A very very very very very very very very very very very very long message";
        Context.EventContext["Key3"] = "Another short message";
        Context.Log();
    }

    void Adding_values_via_params()
    {
        Context.EventContext.AddValues(
            new KeyValuePair<string, object>("key1", "value1"),
            new KeyValuePair<string, object>("key2", "value2"));
        Context.Log();
    }

    void Adding_values_array()
    {
        Context.EventContext.AddValues(
            new List<KeyValuePair<string, object>>
            {
                new("key1", "value1"),
                new("key2", "value2")
            });
    }

    void The_context_contains_key_value_pairs()
    {
        Context.EventContext.Contains("key1").Should().BeTrue();
        Context.EventContext.Contains("key2").Should().BeTrue();
        Context.EventContext["key1"].Should().Be("value1");
        Context.EventContext["key2"].Should().Be("value2");
    }

    void The_context_contains_counts()
    {
        Context.SingleLogMessage.Contains("foo=1").Should().BeTrue();
        Context.SingleLogMessage.Contains("bar=2").Should().BeTrue();
    }

    void Including_an_exception()
    {
        Context.EventContext.IncludeException(new NullReferenceException());
    }

    void Including_an_informational_exception()
    {
        Context.EventContext.IncludeInformationalException(new NullReferenceException(),
            "InfoException");
    }

    void The_context_contains_exception_data(string keyPrefix)
    {
        Context.EventContext.Contains($"{keyPrefix}_Type").Should().BeTrue();
        Context.EventContext.Contains($"{keyPrefix}_Message").Should().BeTrue();
        Context.EventContext.Contains($"{keyPrefix}_StackTrace").Should().BeTrue();
        Context.EventContext.Contains($"{keyPrefix}").Should().BeTrue();
    }

    void The_context_level_is(Level expectedLevel)
    {
        Context.EventContext.Level.Should().Be(expectedLevel);
    }

    class TestStructure
    {
        public int Data1 { get; } = 1;
        public string Data2 { get; } = "foo";
    }

    void Including_a_structure()
    {
        Context.EventContext.IncludeStructure(new TestStructure(), "Prefix");
    }

    void The_context_contains_structure_data()
    {
        Context.EventContext.Contains("Prefix_Data1").Should().BeTrue();
        Context.EventContext.Contains("Prefix_Data2").Should().BeTrue();
        Context.EventContext["Prefix_Data1"].Should().Be(1);
        Context.EventContext["Prefix_Data2"].Should().Be("foo");
    }

    void Adding_counts()
    {
        Context.EventContext.Count("foo");
        Context.EventContext.Count("bar");
        Context.EventContext.Count("bar");
        Context.Log();
    }
}

public class EventContextFieldConflict : Scenarios<EventContextTestContext>
{
    protected override Task BeforeAsync()
    {
        Context.Initialize();
        return base.BeforeAsync();
    }

    [Scenario]
    public void Default()
    {
        Given(Field_conflict_behavior_not_specified);
        When(Logging_duplicate_field_values);
        Then(Field_value_should_be, "value2");
    }

    [Scenario]
    public void Overwrite()
    {
        var behavior = FieldConflict.Overwrite;
        Given(Field_behavior_is, behavior);
        When(Logging_duplicate_field_values_with, behavior);
        Then(Field_value_should_be, "value2");
    }

    [Scenario]
    public void Ignore()
    {
        var behavior = FieldConflict.Ignore;
        Given(Field_behavior_is, behavior);
        When(Logging_duplicate_field_values_with, behavior);
        Then(Field_value_should_be, "value1");
    }

    void Logging_duplicate_field_values()
    {
        Context.EventContext.Set("key", "value1");
        Context.EventContext.Set("key", "value2");
    }

    void Logging_duplicate_field_values_with(FieldConflict behavior)
    {
        Context.EventContext.Set("key", "value1", behavior);
        Context.EventContext.Set("key", "value2", behavior);
    }

    void Field_value_should_be(string expectedValue)
    {
        Context.EventContext["key"].Should().Be(expectedValue);
    }

    static void Field_conflict_behavior_not_specified()
    {
    }

    static void Field_behavior_is(FieldConflict behavior)
    {
    }
}

public class EventContextTrySet : Scenarios<EventContextTestContext>
{
    protected override Task BeforeAsync()
    {
        Context.Initialize();
        return base.BeforeAsync();
    }

    [Scenario]
    public void OnSuccess()
    {
        When(Function_doesnt_throw);
        Then(Value_is_logged);
    }

    [Scenario]
    public void OnException()
    {
        When(Function_throws);
        Then(Value_is_omitted);
    }

    void Function_doesnt_throw()
    {
        Context.EventContext.TrySet("key", () => "value");
    }

    void Function_throws()
    {
        Context.EventContext.TrySet("key", () => throw new Exception());
    }

    void Value_is_logged()
    {
        Context.EventContext.Contains("key").Should().BeTrue();
    }

    void Value_is_omitted()
    {
        Context.EventContext.Contains("key").Should().BeFalse();
    }
}

public class EventContextForgiveNonExistentFields : Scenarios<EventContextTestContext>
{
    protected override Task BeforeAsync()
    {
        Context.Initialize();
        return base.BeforeAsync();
    }

    object _value;

    [Scenario]
    public void Avoid_exceptions()
    {
        When(Attempting_to_get_non_existent_field);
        Then(No_exception_is_thrown)
            .And(Empty_string_is_returned_instead);
    }

    void Attempting_to_get_non_existent_field()
    {
        _value = Context.EventContext["NonExistentKey"];
    }

    void No_exception_is_thrown()
    {
    }

    void Empty_string_is_returned_instead()
    {
        _value.Should().Be(string.Empty);
    }
}

public class CustomTimestamp : Scenarios<EventContextTestContext>
{
    protected override Task BeforeAsync()
    {
        Context.Initialize();
        return base.BeforeAsync();
    }

    [Scenario]
    public void Customize()
    {
        When(Customizing_timestamp);
        Then(time_is_custom_value);
    }

    [Scenario]
    public void DefaultBehavior()
    {
        When(Doing_nothing);
        Then(time_is_now);
    }

    void Customizing_timestamp()
    {
        Context.EventContext.CustomTimestamp = DateTime.Parse("1/1/2000");
    }

    void time_is_custom_value()
    {
        Context.Log();
        Context.SingleLogEvent.Timestamp.Should().Be(DateTime.Parse("1/1/2000"));
    }

    void Doing_nothing() {}

    void time_is_now()
    {
        Context.Log();
        Context.SingleLogEvent.Timestamp.Should().BeWithin(TimeSpan.FromMinutes(1));
    }
}

public class SetComponent : Scenarios<EventContextTestContext>
{
    protected override Task BeforeAsync()
    {
        Context.Initialize();
        return base.BeforeAsync();
    }

    [Scenario]
    public void UsingProperty()
    {
        When(Setting_via_property);
        Then(Value_is_reflected);
    }

    [Scenario]
    public void UsingIndexer()
    {
        When(Setting_via_indexer);
        Then(Value_is_reflected);
    }

    const string CUSTOM_VALUE = "foobar123";
    void Setting_via_property()
    {
        Context.EventContext.Component = CUSTOM_VALUE;
    }

    void Setting_via_indexer()
    {
        Context.EventContext["Component"] = CUSTOM_VALUE;
    }


    void Value_is_reflected()
    {
        Context.EventContext.Component.Should().Be(CUSTOM_VALUE);
        Context.Log();
        Context.SingleLogMessage.Should().Contain(CUSTOM_VALUE);
    }
}

public class SetOperation : Scenarios<EventContextTestContext>
{
    protected override Task BeforeAsync()
    {
        Context.Initialize();
        return base.BeforeAsync();
    }

    [Scenario]
    public void UsingProperty()
    {
        When(Setting_via_property);
        Then(Value_is_reflected);
    }

    [Scenario]
    public void UsingIndexer()
    {
        When(Setting_via_indexer);
        Then(Value_is_reflected);
    }

    const string CUSTOM_VALUE = "foobar123";
    void Setting_via_property()
    {
        Context.EventContext.Operation = CUSTOM_VALUE;
    }

    void Setting_via_indexer()
    {
        Context.EventContext["Operation"] = CUSTOM_VALUE;
    }

    void Value_is_reflected()
    {
        Context.EventContext.Operation.Should().Be(CUSTOM_VALUE);
        Context.Log();
        Context.SingleLogMessage.Should().Contain(CUSTOM_VALUE);
    }
}
