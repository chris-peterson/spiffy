# Spiffy
A monitoring framework for .NET that supports IoC and modern targets, e.g. Splunk

## Setup
`PM> Install-Package Spiffy.Monitoring.NLog`

## Initialize
```c#
        static void Main()
        {
            // set any global key-value-pairs
            GlobalEventContext.Instance.Set("Application", "TestConsole");

            // Initialize to use NLog (currently the only supported target)
            NLog.Initialize();
        }

```
