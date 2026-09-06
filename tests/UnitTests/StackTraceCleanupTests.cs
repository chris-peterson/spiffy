using AwesomeAssertions;
using Kekiri.Xunit;
using Spiffy.Monitoring;

namespace UnitTests;

public class StackTraceCleanupTests : Scenarios
{
    [ScenarioOutline]
    [Example(
        "   at MyApp.Service.<FetchAsync>d__12.MoveNext()",
        "   at async MyApp.Service.FetchAsync()",
        "async state machine frames name the method the caller wrote")]
    [Example(
        "   at MyApp.Service.<Handle>b__3_0()",
        "   at MyApp.Service.Handle { lambda }()",
        "lambda frames name their enclosing method")]
    [Example(
        "   at MyApp.Service.<Handle>g__Validate|0_0()",
        "   at MyApp.Service.Handle { Validate }()",
        "local function frames name both the method and the function")]
    [Example(
        "   at MyApp.Service.Handle()",
        "   at MyApp.Service.Handle()",
        "an ordinary frame is left alone")]
    public void Compiler_generated_frames_are_simplified(string input, string expected, string reason)
    {
        When(Simplifying, input);
        Then(It_should_be, expected, reason);
    }

    [Scenario]
    public void Empty_input()
    {
        When(Simplifying, "");
        Then(It_should_be, "", "an empty stack trace is returned as-is");
    }

    string _result;

    void Simplifying(string stackTrace)
    {
        _result = StackTraceCleanup.Simplify(stackTrace);
    }

    void It_should_be(string expected, string reason)
    {
        _result.Should().Be(expected, because: reason);
    }
}
