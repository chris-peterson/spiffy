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

## Multiple Providers

Multiple providers can be supplied, for example, this application uses both `Console` (built-in), as well as `File` (NLog)

```csharp
Spiffy.Monitoring.Configuration.Initialize(spiffy => {
    spiffy.Providers
        .Console()
        .NLog(nlog => nlog.Targets(t => t.File()));
});
```
