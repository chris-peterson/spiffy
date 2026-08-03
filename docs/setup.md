# Setup

```bash
PM> Install-Package Spiffy.Monitoring
```

## Built-In Logging Providers

`Spiffy.Monitoring` includes "built-in" logging mechanisms (`Trace` and `Console`).

There is no default logging behavior, you must initialize provider(s) by calling `Spiffy.Monitoring.Configuration.Initialize`.

Until initialized, any published `EventContext` will not be observable, so it is recommended that initialization be as early as possible when your application is starting (i.e. in the entry point).

```csharp
Configuration.Initialize(spiffy => { spiffy.Providers.Console(); });
```

## Extended Providers

For extended functionality, you'll need to install a "provider package".

NOTE: the provider package need only be installed for your application's entry point assembly, it need not be installed in library packages.

### NLog Provider

```bash
PM> Install-Package Spiffy.Monitoring.NLog
```

```csharp
static void Main() {
    // this should be the first line of your application
    Spiffy.Monitoring.Configuration.Initialize(spiffy => {
        spiffy.Providers
            .NLog(nlog => nlog.Targets(t => t.File()));
    });
}
```

### AWS Provider

```bash
PM> Install-Package Spiffy.Monitoring.Aws
```

Routes the AWS SDK's own diagnostics into your structured logs as `Component=AwsSdk Operation=Event`.

```csharp
static void Main() {
    Spiffy.Monitoring.Configuration.Initialize(spiffy => {
        spiffy.Providers.Aws();
    });
}
```

Each event's level comes from the severity the SDK reported it at.

Where the SDK hands the exception to the trace listener, it is broken out into `Exception_*` fields.
Only v3 of the AWS SDK does so: v4 discards the exception before the listener sees it, keeping only
the message text. So don't build alerting on `Exception_*` for a v4 application.

The SDK reports a lot at informational severity, including several messages that repeat on every call
and carry nothing actionable: its own user-agent string, request-signing canonicalization, retry
bookkeeping, buffer resizing, DynamoDB table descriptions, and one line per unset `AWS_*` environment
variable. Those are filtered by default; anything the SDK reports at warning or above never is.

Retry and failure notices are kept, including the ones the SDK reports at informational severity, so
a throttled DynamoDB request or a rejected S3 request still reaches your logs.

There's one filter per kind, and each kind is a member of `SdkNoise`, so you don't have to know the
message text. Remove a filter and that kind shows up in your logs again:

```csharp
spiffy.Providers.Aws(c => c.NoiseFilters.Remove(SdkNoise.UserAgentHeader));
```

To filter noise `SdkNoise` doesn't name (something specific to your application, or a message a
newer SDK started reporting), add a filter matching the start of the message:

```csharp
spiffy.Providers.Aws(c => c.NoiseFilters.Add("Credentials found using"));
```

`Clear` removes every filter, so everything the SDK reports is logged. Useful when you're working out
what to filter:

```csharp
spiffy.Providers.Aws(c => c.NoiseFilters.Clear());
```

Response bodies are logged on error only. `Always` logs them for every call, `Never` for none:

```csharp
spiffy.Providers.Aws(c => c.LogResponses.Always());
```

## Multiple Providers

Multiple providers can be supplied, for example, this application uses both `Console` (built-in), as well as `File` (NLog)

```csharp
Spiffy.Monitoring.Configuration.Initialize(spiffy => {
    spiffy.Providers
        .Console()
        .NLog(nlog => nlog.Targets(t => t.File()));
});
```
