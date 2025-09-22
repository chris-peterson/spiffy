using System.Linq;
using System.Collections.Generic;
using AwesomeAssertions;
using Kekiri.Xunit;
using Spiffy.Monitoring;

namespace UnitTests;

public class Naming : Scenarios
{
    TestingContext _context;
    enum FieldNaming
    {
        Short
    }

    [Scenario]
    public void Legacy_naming_is_the_default_behavior()
    {
        Given(unspecified_field_naming_convention);
        When(logging);
        Then(it_uses_legacy_field_names);
    }

    [Scenario]
    public void Short_naming_is_an_option()
    {
        Given(short_field_naming_convention_chosen);
        When(logging);
        Then(it_uses_short_field_names);
    }

    void unspecified_field_naming_convention()
    {
        _context =new TestingContext();
    }

    void short_field_naming_convention_chosen()
    {
        _context =new TestingContext(FieldNaming.Short);
    }

    void logging()
    {
        _context.EventContext.Dispose();
    }

    void it_uses_legacy_field_names()
    {
        var logEvent = _context.LogEvents.Single();
        foreach (var expectedFieldName in new[] { "Level", "Component", "Operation", "TimeElapsed" })
        {
            logEvent.Properties.Should().ContainKey(expectedFieldName);
            logEvent.MessageWithTime.Should().Contain($"{expectedFieldName}=");
        }
    }

    void it_uses_short_field_names()
    {
        var logEvent = _context.LogEvents.Single();
        foreach (var expectedFieldName in new[] { "l", "c", "o", "ms" })
        {
            logEvent.Properties.Should().ContainKey(expectedFieldName);
            logEvent.MessageWithTime.Should().Contain($"{expectedFieldName}=");
        }
    }

    class TestingContext
    {
        public TestingContext(FieldNaming? fieldNaming = null)
        {
            var config = Configuration.Initialize(customize =>
            {
                if (fieldNaming == FieldNaming.Short)
                {
                    customize.Naming.UseShortFieldNames();
                }
                customize.Providers.Add("test", logEvent => LogEvents.Add(logEvent));
            });
            EventContext = new EventContext("TestComponent", "TestOperation", config);
        }

        public EventContext EventContext { get; }
        public List<LogEvent> LogEvents { get; } = [];
    }
}
