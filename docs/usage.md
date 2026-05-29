# Usage

## Example Program

```csharp
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

## Normal Entry

```text
[2014-06-13 00:05:17.634Z] Application=MyApplication Level=Info Component=Program Operation=Main TimeElapsed=1004.2 Key=Value TimeElapsed_LongRunning=1000.2
```

## Exception Entry

```text
[2014-06-13 00:12:52.038Z] Application=MyApplication Level=Error Component=Program Operation=Main TimeElapsed=1027.0 Key=Value ErrorReason="An exception has occurred" Exception_Type=ApplicationException Exception_Message="you were unlucky!" Exception_StackTrace="..." InnermostException_Type=NullReferenceException InnermostException_Message="Object reference not set to an instance of an object." Exception="See Exception_* and InnermostException_* for more details" TimeElapsed_LongRunning=1000.0
```

## Hosting Frameworks

`Spiffy.Monitoring` is designed to be easy to use in any context.

The most basic usage is to instrument a specific method.
This can be achieved by "newing up" an `EventContext`.
This usage mode results in `Component` being set to the containing code's
class name, and `Operation` is set to the containing code's method name.

There are times when you may want to instrument something that's not
a specific method. One such example is an API — in this context,
you might want to have 1 log event per request. Consider setting
`Component` to be the controller name, and `Operation`
the action name. To achieve this, add middleware that calls
`EventContext.Initialize` with the desired labels.
