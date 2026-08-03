using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AwesomeAssertions;
using Kekiri.Xunit;
using Spiffy.Monitoring;
using Spiffy.Monitoring.Aws;
using Xunit;

namespace UnitTests;

// The listener builds its EventContext from the ambient Configuration, so these scenarios
// have to take turns installing a capturing provider into Configuration.Default.
[CollectionDefinition(AwsListener.Collection, DisableParallelization = true)]
public class AwsListener
{
    public const string Collection = "AwsListener";
}

public class AwsListenerTestContext
{
    readonly List<LogEvent> _loggedEvents = new();
    AwsProvider.AwsEvent _listener;

    public void Initialize(Action<AwsConfigurationApi> configure = null)
    {
        _loggedEvents.Clear();
        Configuration.Initialize(spiffy => spiffy.Providers.Add("test", _loggedEvents.Add));

        var config = new AwsConfigurationApi();
        configure?.Invoke(config);
        _listener = new AwsProvider.AwsEvent(config.SuppressMessages.Prefixes);
    }

    // Mirrors how the SDK reports.  A lone message is what v4 always sends -- its
    // DiagnosticAdaptorLogger accepts the exception and then drops it -- and what v3 sends
    // for InfoFormat/DebugFormat.
    public void Report(TraceEventType eventType, string message)
    {
        _listener.TraceData(null, "Amazon", eventType, 0, message);
    }

    // Message plus exception is v3 only, from Error/Debug.
    public void Report(TraceEventType eventType, string message, Exception exception)
    {
        _listener.TraceData(null, "Amazon", eventType, 0, message, exception);
    }

    public IReadOnlyList<LogEvent> LoggedEvents => _loggedEvents;
    public LogEvent SingleLogEvent => _loggedEvents.Single();
}

[Collection(AwsListener.Collection)]
public class AwsListenerSeverity : Scenarios<AwsListenerTestContext>
{
    const string ErrorMessage = "An exception of type HttpErrorResponseException was handled in ErrorHandler.";

    [Scenario]
    public void Errors_are_reported_as_errors()
    {
        Given(A_listener);
        When(The_sdk_reports, TraceEventType.Error, ErrorMessage);
        Then(One_event_is_logged, ErrorMessage)
            .And(The_level_is, Level.Error);
    }

    [Scenario]
    public void Critical_is_reported_as_error()
    {
        Given(A_listener);
        When(The_sdk_reports, TraceEventType.Critical, "Something dire");
        Then(One_event_is_logged, "Something dire")
            .And(The_level_is, Level.Error);
    }

    [Scenario]
    public void Warnings_are_reported_as_warnings()
    {
        Given(A_listener);
        When(The_sdk_reports, TraceEventType.Warning, "Something suspect");
        Then(One_event_is_logged, "Something suspect")
            .And(The_level_is, Level.Warning);
    }

    [Scenario]
    public void Information_is_reported_as_info()
    {
        Given(A_listener);
        When(The_sdk_reports, TraceEventType.Information, "Credentials found using environment variables.");
        Then(One_event_is_logged, "Credentials found using environment variables.")
            .And(The_level_is, Level.Info);
    }

    // The message the old substring heuristic was meant to catch is 76 characters long, so
    // its `IndexOf("exception", Math.Min(100, length))` start offset landed past the end.
    [Scenario]
    public void Severity_classifies_errors_the_substring_heuristic_missed()
    {
        Given(A_listener);
        When(The_sdk_reports, TraceEventType.Error, ErrorMessage);
        Then(The_level_is, Level.Error)
            .And(The_message_does_not_announce_its_own_severity);
    }

    void The_message_does_not_announce_its_own_severity()
    {
        ErrorMessage.IndexOf("exception", Math.Min(100, ErrorMessage.Length), StringComparison.OrdinalIgnoreCase)
            .Should().Be(-1, because: "otherwise this test isn't covering what the heuristic missed");
    }

    void A_listener()
    {
        Context.Initialize();
    }

    void The_sdk_reports(TraceEventType eventType, string message)
    {
        Context.Report(eventType, message);
    }

    void One_event_is_logged(string expectedMessage)
    {
        Context.SingleLogEvent.Message.Should().Contain(expectedMessage);
    }

    void The_level_is(Level expectedLevel)
    {
        Context.SingleLogEvent.Level.Should().Be(expectedLevel);
    }
}

[Collection(AwsListener.Collection)]
public class AwsListenerExceptions : Scenarios<AwsListenerTestContext>
{
    // Only reachable on SDK v3, which is why the package floor still matters: v4 drops the
    // exception inside the SDK, so a v4 application gets the message text and no Exception_* fields.
    [Scenario]
    public void Errors_carry_exception_detail()
    {
        Given(A_listener);
        When(The_sdk_reports_an_exception_at, TraceEventType.Error);
        Then(The_event_has_exception_fields)
            .And(The_level_is, Level.Error);
    }

    // v3 reports Debug(exception, ...) at Verbose; an exception arriving at that
    // severity shouldn't escalate the event to an error.
    [Scenario]
    public void Verbose_exceptions_do_not_escalate()
    {
        Given(A_listener);
        When(The_sdk_reports_an_exception_at, TraceEventType.Verbose);
        Then(The_event_has_exception_fields)
            .And(The_level_is, Level.Info);
    }

    void A_listener()
    {
        Context.Initialize();
    }

    void The_sdk_reports_an_exception_at(TraceEventType eventType)
    {
        Context.Report(eventType, "Something went sideways", new TimeoutException("boom"));
    }

    void The_event_has_exception_fields()
    {
        var properties = Context.SingleLogEvent.Properties;
        properties.Should().ContainKey("Exception_Type");
        properties["Exception_Type"].Should().Be(nameof(TimeoutException));
        properties.Should().ContainKey("Exception_Message");
        properties["Exception_Message"].Should().Contain("boom");
    }

    void The_level_is(Level expectedLevel)
    {
        Context.SingleLogEvent.Level.Should().Be(expectedLevel);
    }
}

[Collection(AwsListener.Collection)]
public class AwsListenerSuppression : Scenarios<AwsListenerTestContext>
{
    // The messages here are taken verbatim from a production log query, so a change to the
    // prefixes has to keep clearing the traffic that motivated them.
    [ScenarioOutline]
    [Example("User-Agent Header: aws-sdk-dotnet-coreclr/4.0.9.6 ua/2.1 os/linux#6.1.175.219 md/ARCH#Arm64 api/DynamoDB#4.0.9.6")]
    [Example("Resizing buffer from 4096 to 8192")]
    [Example("Resolved DefaultConfigurationMode for RegionEndpoint [us-east-1] to [Standard].")]
    [Example("Description for table [my-table] loaded from SDK Cache")]
    [Example("The environment variable AWS_MAX_ATTEMPTS was not set with a value.")]
    [Example("Retry check: IsStaleConnectionError=False, CanRetry=True, staleConnectionRetries=0, maxStaleConnectionRetries=10, IsRequestStreamRewindable=True")]
    [Example("Single encoded /{Key+} with endpoint https://example.s3.us-west-2.amazonaws.com/ for canonicalization: /public/getty/workbench-pages/index.xml.gz")]
    [Example("Double encoded /usageplans with endpoint https://apigateway.us-west-2.amazonaws.com/ for canonicalization: /usageplans")]
    public void Noise_is_suppressed_by_default(string message)
    {
        Given(A_listener);
        When(The_sdk_reports_at_information, message);
        Then(Nothing_is_logged);
    }

    [Scenario]
    public void Unrecognized_information_still_flows()
    {
        Given(A_listener);
        When(The_sdk_reports_at_information, "Region found using environment variable.");
        Then(One_event_is_logged);
    }

    // Failures and throttling arrive at Information too -- RetryHandler uses InfoFormat -- so
    // they survive only because the filter denies rather than allows.  Note the contrast with
    // the suppressed "Retry check: " bookkeeping: a retry *notice* names what went wrong.
    [ScenarioOutline]
    [Example("ProvisionedThroughputExceededException making request BatchGetItemRequest to https://dynamodb.us-west-2.amazonaws.com/. Attempting retry 1 of 10.")]
    [Example("AmazonS3Exception making request GetObjectMetadataRequest to https://example.s3.us-west-2.amazonaws.com/. Attempt 1.")]
    [Example("UnsupportedLanguagePairException making request TranslateTextRequest to https://translate.us-east-1.amazonaws.com/. Attempting retry 1 of 4.")]
    public void Failure_notices_at_information_still_flow(string message)
    {
        Given(A_listener);
        When(The_sdk_reports_at_information, message);
        Then(One_event_is_logged);
    }

    [Scenario]
    public void Suppression_never_applies_to_errors()
    {
        Given(A_listener);
        When(The_sdk_reports_an_error, "User-Agent Header: something went wrong while building it");
        Then(One_event_is_logged);
    }

    static readonly Action<AwsConfigurationApi> SuppressNothing = c => c.SuppressMessages.None();
    static readonly Action<AwsConfigurationApi> AlsoSuppressRegion = c => c.SuppressMessages.Add("Region found using");
    static readonly Action<AwsConfigurationApi> OnlySuppressRegion = c => c.SuppressMessages.Only("Region found using");

    static readonly Action<AwsConfigurationApi> AlsoSuppressRegionAndCredentials = c =>
    {
        c.SuppressMessages.Add("Region found using");
        c.SuppressMessages.Add("Credentials found using");
    };

    [Scenario]
    public void None_disables_suppression()
    {
        Given(A_listener_configured_with, SuppressNothing);
        When(The_sdk_reports_at_information, "User-Agent Header: aws-sdk-dotnet-coreclr/4.0.2.11");
        Then(One_event_is_logged);
    }

    [Scenario]
    public void Add_extends_the_defaults()
    {
        Given(A_listener_configured_with, AlsoSuppressRegion);
        When(The_sdk_reports_at_information, "Region found using environment variable.");
        Then(Nothing_is_logged);
    }

    [Scenario]
    public void Add_keeps_the_defaults()
    {
        Given(A_listener_configured_with, AlsoSuppressRegion);
        When(The_sdk_reports_at_information, "User-Agent Header: aws-sdk-dotnet-coreclr/4.0.2.11");
        Then(Nothing_is_logged);
    }

    [Scenario]
    public void Add_accumulates_across_calls()
    {
        Given(A_listener_configured_with, AlsoSuppressRegionAndCredentials);
        When(The_sdk_reports_at_information, "Region found using environment variable.");
        Then(Nothing_is_logged);
    }

    [Scenario]
    public void Only_discards_the_defaults()
    {
        Given(A_listener_configured_with, OnlySuppressRegion);
        When(The_sdk_reports_at_information, "User-Agent Header: aws-sdk-dotnet-coreclr/4.0.2.11");
        Then(One_event_is_logged);
    }

    [Scenario]
    public void Only_suppresses_what_it_names()
    {
        Given(A_listener_configured_with, OnlySuppressRegion);
        When(The_sdk_reports_at_information, "Region found using environment variable.");
        Then(Nothing_is_logged);
    }

    [Scenario]
    public void Blank_messages_are_dropped()
    {
        Given(A_listener);
        When(The_sdk_reports_at_information, "   ");
        Then(Nothing_is_logged);
    }

    void A_listener()
    {
        Context.Initialize();
    }

    void A_listener_configured_with(Action<AwsConfigurationApi> configure)
    {
        Context.Initialize(configure);
    }

    void The_sdk_reports_at_information(string message)
    {
        Context.Report(TraceEventType.Information, message);
    }

    void The_sdk_reports_an_error(string message)
    {
        Context.Report(TraceEventType.Error, message);
    }

    void Nothing_is_logged()
    {
        Context.LoggedEvents.Should().BeEmpty();
    }

    void One_event_is_logged()
    {
        Context.LoggedEvents.Should().HaveCount(1);
    }
}
