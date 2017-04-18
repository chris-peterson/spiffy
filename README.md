# Spiffy
A monitoring framework for .NET that supports IoC and modern targets, e.g. Splunk

## Status
Package | Build Status | Latest Release 
:-------- | :------------ | :------------ 
Spiffy.Monitoring | [![Build status](https://ci.appveyor.com/api/projects/status/251sp958bhrbxpwk?svg=true)](https://ci.appveyor.com/project/chris-peterson/spiffy) | [![NuGet version](https://img.shields.io/nuget/v/Spiffy.Monitoring.svg)](https://badge.fury.io/nu/spiffy.monitoring)
Spiffy.Monitoring.NLog | [![Build status](https://ci.appveyor.com/api/projects/status/251sp958bhrbxpwk?svg=true)](https://ci.appveyor.com/project/chris-peterson/spiffy) | [![NuGet version](https://img.shields.io/nuget/vpre/Spiffy.Monitoring.NLog.svg)](https://badge.fury.io/nu/spiffy.monitoring.nlog)

## Setup
`PM> Install-Package Spiffy.Monitoring`

If you need additional customization (anything more than just logging to the console), you'll need to install a provider package and intialize it.  NOTE: the provider package need only be installed for your application's entry point assembly, it is not necessary to be installed in library packages.

`PM> Install-Package Spiffy.Monitoring.NLog`

## Example

```c#
        static void Main()
        {
            // this should be the first line of your application.  If not provided, the default behavior is to emit logs to the console.
            NLog.Initialize();

            // key-value-pairs set here appear in every event message
            GlobalEventContext.Instance
                .Set("Application", "TestConsole");

            using (var context = new EventContext())
            {
                context["MyCustomValue"] = "foo";

                using (context.Time("LongRunning"))
                {
                    DoSomethingLongRunning();
                }

                try
                {
                    DoSomethingDangerous();
                }
                catch (Exception ex)
                {
                    context.IncludeException(ex);
                }
            }
        }
```

## Log
The example above creates a file named `current.log` in a `Logs` subfolder of the application's running directory.  It contains entries like the following:

### Normal Entry
[2014-06-13 00:05:17.634Z] Application=TestConsole **Level=Info** Component=Program Operation=Main TimeElapsed=1004.2 **MyCustomValue=foo** TimeElapsed_LongRunning=1000.2
  
### Exception Entry
[2014-06-13 00:12:52.038Z] Application=TestConsole **Level=Error** Component=Program Operation=Main TimeElapsed=1027.0 MyCustomValue=foo **ErrorReason="An exception has ocurred"** **Exception_Type=ApplicationException Exception_Message="you were unlucky!"** Exception_StackTrace="   at TestConsoleApp.Program.DoSomethingDangerous() in c:\src\git\github\chris-peterson\Spiffy\src\Tests\TestConsoleApp\Program.cs:line 47
   at TestConsoleApp.Program.Main() in c:\src\git\github\chris-peterson\Spiffy\src\Tests\TestConsoleApp\Program.cs:line 29" InnermostException_Type=NullReferenceException **InnermostException_Message="Object reference not set to an instance of an object."** InnermostException_StackTrace={null} Exception="See Exception_* and InnermostException_* for more details" TimeElapsed_LongRunning=1000.0
