# Spiffy
A monitoring framework for .NET that supports IoC and modern targets, e.g. Splunk

## Setup
`PM> Install-Package Spiffy.Monitoring.NLog`

## Example

```c#
        static void Main()
        {
            // this should be the first line of your application
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


### Normal Entry
```
[2014-06-13 00:05:17.634Z] Application=TestConsole Level=Info Component=Program Operation=Main TimeElapsed=1004.2 MyCustomValue=foo TimeElapsed_LongRunning=1000.2
```

### Exception Entry:
```
[2014-06-13 00:12:52.038Z] Application=TestConsole Level=Error Component=Program Operation=Main TimeElapsed=1027.0 MyCustomValue=foo ErrorReason="An exception has ocurred" Exception_Type=ApplicationException Exception_Message="you were unlucky!" Exception_StackTrace="   at TestConsoleApp.Program.DoSomethingDangerous() in c:\src\git\github\chris-peterson\Spiffy\src\Tests\TestConsoleApp\Program.cs:line 47
   at TestConsoleApp.Program.Main() in c:\src\git\github\chris-peterson\Spiffy\src\Tests\TestConsoleApp\Program.cs:line 29" InnermostException_Type=NullReferenceException InnermostException_Message="Object reference not set to an instance of an object." InnermostException_StackTrace={null} Exception="See Exception_* and InnermostException_* for more details" TimeElapsed_LongRunning=1000.0
```
