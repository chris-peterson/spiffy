# Overview

A structured logging framework for .NET that supports log analysis (e.g. Splunk) and metrics gathering (e.g. Prometheus).

Battle-tested in high-volume production environments for more than 10 years, handling over 1,000,0000,000,000 (1 trillion) requests.

## Status

[![build](https://github.com/chris-peterson/spiffy/actions/workflows/ci.yml/badge.svg)](https://github.com/chris-peterson/spiffy/actions/workflows/ci.yml)

Package | Latest Release |
:-------- | :------------ |
`Spiffy.Monitoring` | [![NuGet version](https://img.shields.io/nuget/dt/Spiffy.Monitoring.svg)](https://www.nuget.org/packages/spiffy.monitoring)
`Spiffy.Monitoring.Aws` | [![NuGet version](https://img.shields.io/nuget/dt/Spiffy.Monitoring.Aws.svg)](https://www.nuget.org/packages/spiffy.monitoring.aws)
`Spiffy.Monitoring.NLog` | [![NuGet version](https://img.shields.io/nuget/dt/Spiffy.Monitoring.NLog.svg)](https://www.nuget.org/packages/spiffy.monitoring.nlog)
`Spiffy.Monitoring.Prometheus` | [![NuGet version](https://img.shields.io/nuget/dt/Spiffy.Monitoring.Prometheus.svg)](https://www.nuget.org/packages/spiffy.monitoring.prometheus)
`Spiffy.Monitoring.Splunk` | [![NuGet version](https://img.shields.io/nuget/dt/Spiffy.Monitoring.Splunk.svg)](https://www.nuget.org/packages/spiffy.monitoring.splunk)

## Setup

`PM> Install-Package Spiffy.Monitoring`

### Built-In Logging Providers

`Spiffy.Monitoring` includes "built-in" logging mechanisms (`Trace` and `Console`).

There is no default logging behavior, you must initialize provider(s) by calling `Spiffy.Monitoring.Configuration.Initialize`.

Until initialized, any published `EventContext` will not be observable, so it is recommended that initialization be as early as possible when your application is starting (i.e. in the entry point).

**Example**

```c#
  Configuration.Initialize(spiffy => { spiffy.Providers.Console(); });
```

### Extended Providers

For extended functionality, you'll need to install a "provider package".

NOTE: the provider package need only be installed for your application's entry point assembly, it need not be installed in library packages.

#### NLog Provider

`PM> Install-Package Spiffy.Monitoring.NLog`

**Example**

```c#
    static void Main() {
        // this should be the first line of your application
        Spiffy.Monitoring.Configuration.Initialize(spiffy => {
            spiffy.Providers
                .NLog(nlog => nlog.Targets(t => t.File()));
        });
    }
```

### Multiple Providers

Multiple providers can be provied, for example, this application uses both `Console` (built-in), as well as `File` (NLog)

**Example**

```c#
    Spiffy.Monitoring.Configuration.Initialize(spiffy => {
        spiffy.Providers
            .Console()
            .NLog(nlog => nlog.Targets(t => t.File()));
    });
```

## Log

### Example Program

```c#
        // key-value-pairs set here appear in every event message
        GlobalEventContext.Instance
            .Set("Application", "MyApplication");

        using (var context = new EventContext()) {
            context["Key"] = "Value";

            using (context.Time("LongRunning")) {
                DoSomethingLongRunning();
            }

            try {
                DoSomethingDangerous();
            }
            catch (Exception ex) {
                context.IncludeException(ex);
            }
        }
```

### Normal Entry

> [2014-06-13 00:05:17.634Z] Application=MyApplication **Level=Info** Component=Program Operation=Main TimeElapsed=1004.2 **Key=Value** TimeElapsed_LongRunning=1000.2

### Exception Entry

> [2014-06-13 00:12:52.038Z] Application=MyApplication **Level=Error** Component=Program Operation=Main TimeElapsed=1027.0 Key=Value **ErrorReason="An exception has ocurred"** **Exception_Type=ApplicationException Exception_Message="you were unlucky!"** Exception_StackTrace="   at TestConsoleApp.Program.DoSomethingDangerous() in c:\src\git\github\chris-peterson\Spiffy\src\Tests\TestConsoleApp\Program.cs:line 47
   at TestConsoleApp.Program.Main() in c:\src\git\github\chris-peterson\Spiffy\src\Tests\TestConsoleApp\Program.cs:line 29" InnermostException_Type=NullReferenceException **InnermostException_Message="Object reference not set to an instance of an object."** InnermostException_StackTrace={null} Exception="See Exception_* and InnermostException_* for more details" TimeElapsed_LongRunning=1000.0

## Hosting Frameworks

`Spiffy.Monitoring` is designed to be easy to use in any context.

The most basic usage is to instrument a specific method.
This can be achieved by "newing up" an `EventContext`.
This usage mode results in `Component` being set to the containing code's
class name, and `Operation` is set to the containing code's method name

There are times when you may want to instrument something that's not
a specific method.  One such example is an API -- in this context,
you might want to have 1 log event per request.  Consider setting
`Component` to be the controller name, and `Operation`
the action name.  To acheive this, add middleware that calls
`EventContext.Initialize` with the desired labels.
