# Overview

A structured logging framework for .NET that supports log aggregation, e.g. Splunk.

Handled over **219,000,000,000** production requests (and counting!)

## Status

[![Build status](https://gitlab.com/chris-peterson/spiffy/badges/master/pipeline.svg)](https://gitlab.com/chris-peterson/spiffy/-/pipelines)

Package | Latest Release |
:-------- | :------------ |
Spiffy.Monitoring | [![NuGet version](https://img.shields.io/nuget/dt/Spiffy.Monitoring.svg)](https://www.nuget.org/packages/spiffy.monitoring)
Spiffy.Monitoring.NLog | [![NuGet version](https://img.shields.io/nuget/dt/Spiffy.Monitoring.NLog.svg)](https://www.nuget.org/packages/spiffy.monitoring.nlog)

## Setup

`PM> Install-Package Spiffy.Monitoring`

### Built-In Logging

`Spiffy.Monitoring` includes "built-in" logging mechanisms (`Trace` and `Console`).  There is no default logging behavior, you must
initialize logging.  Early in your application's entry point, call:

```c#
    Behavior.UseBuiltInLogging(BuiltInLogging.Trace)
```

### Custom Logging

For extended functionality, you'll need to install a "provider package".

NOTE: the provider package need only be installed for your application's entry point assembly, it need not be installed in library packages.

#### NLog Provider

`PM> Install-Package Spiffy.Monitoring.NLog`

## Example

```c#
    static void Main() {
        // this should be the first line of your application
         Spiffy.Monitoring.NLog.Initialize(config => {
            config.Targets(t => t.ColoredConsole());
         });

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
    }
```

## Log

The example above creates a file named `current.log` in a `Logs` subfolder of the application's running directory.  It contains entries like the following:

### Normal Entry

[2014-06-13 00:05:17.634Z] Application=MyApplication **Level=Info** Component=Program Operation=Main TimeElapsed=1004.2 **Key=Value** TimeElapsed_LongRunning=1000.2

### Exception Entry

[2014-06-13 00:12:52.038Z] Application=MyApplication **Level=Error** Component=Program Operation=Main TimeElapsed=1027.0 Key=Value **ErrorReason="An exception has ocurred"** **Exception_Type=ApplicationException Exception_Message="you were unlucky!"** Exception_StackTrace="   at TestConsoleApp.Program.DoSomethingDangerous() in c:\src\git\github\chris-peterson\Spiffy\src\Tests\TestConsoleApp\Program.cs:line 47
   at TestConsoleApp.Program.Main() in c:\src\git\github\chris-peterson\Spiffy\src\Tests\TestConsoleApp\Program.cs:line 29" InnermostException_Type=NullReferenceException **InnermostException_Message="Object reference not set to an instance of an object."** InnermostException_StackTrace={null} Exception="See Exception_* and InnermostException_* for more details" TimeElapsed_LongRunning=1000.0
