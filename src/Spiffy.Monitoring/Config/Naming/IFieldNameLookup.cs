using System;

namespace Spiffy.Monitoring.Config.Naming;

internal interface IFieldNameLookup
{
    string Get(Field field);
}

internal class ShortFieldNameLookup : IFieldNameLookup
{
    public string Get(Field field)
    {
        return field switch
        {
            Field.Timestamp => "ts",
            Field.Level => "l",
            Field.Component => "c",
            Field.Operation => "o",
            Field.TimeElapsed => "ms",
            Field.ErrorReason => "msg",
            Field.WarningReason => "msg", // not a copy/pasta mistake; there's no reason to distinguish between error and warning as that's already in Level
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
        };
    }
}

// preserve the 6.x field names.  starting in 7.x, we will use the short field names.
internal class LegacyFieldNameLookup : IFieldNameLookup
{
    public string Get(Field field)
    {
        return field switch
        {
            Field.Timestamp => "time",
            Field.Level => "Level",
            Field.Component => "Component",
            Field.Operation => "Operation",
            Field.TimeElapsed => "TimeElapsed",
            Field.ErrorReason => "ErrorReason",
            Field.WarningReason => "WarningReason",
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
        };
    }
}
