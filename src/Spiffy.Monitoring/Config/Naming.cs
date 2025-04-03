using System;

namespace Spiffy.Monitoring.Config;

internal class Naming
{
    internal interface IFieldNameLookup
    {
        string GetFieldName(Field field);
    }
    
    internal class ShortFieldNameLookup : IFieldNameLookup
    {
        public string GetFieldName(Field field)
        {
            return field switch
            {
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
        public string GetFieldName(Field field)
        {
            return field switch
            {
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

    internal static IFieldNameLookup FieldNameLookup { get; set; } = new LegacyFieldNameLookup();

    public static string Get(Field field)
    {
        return FieldNameLookup.GetFieldName(field);
    }
}

internal enum Field
{
    Level,
    Component,
    Operation,
    TimeElapsed,
    ErrorReason,
    WarningReason
}
