using System;
using System.Linq;
using Spiffy.Monitoring.Config;
using Spiffy.Monitoring.Config.Formatting;
using Spiffy.Monitoring.Config.Naming;

// ReSharper disable once CheckNamespace -- this is intentional to avoid additional nesting
namespace Spiffy.Monitoring
{
    public static class Configuration
    {
        // default values - we should use these if the customization callback hasn't been called, or didn't change values
        internal static Action<EventContext>[] BeforeLoggingActions                         = [];
        internal static Action<LogEvent>[] LoggingActions                                   = [];
        internal static string CustomNullValue { get; private set; }                        = null;
        internal static bool RemoveNewLines { get; private set; }                           = false;
        internal static int DeprioritizedValueLength { get; private set; }                  = 1024;
        internal static IFieldNameLookup FieldNameLookup { get; private set; }                      = new LegacyFieldNameLookup();
        internal static TimestampNaming TimestampNaming { get; private set; }               = TimestampNaming.UseUnnamedFieldInBrackets;
        internal static string TimestampFormatString { get; private set; }                  = "yyyy-MM-dd HH:mm:ss.fffK";
        internal static SpecialValueFormatting SpecialValueFormatting { get; private set; } = SpecialValueFormatting.UseAlternateQuotes;

        public static void Initialize(Action<InitializationApi> customize)
        {
            var api = new InitializationApi();
            if (customize == null)
            {
                throw new Exception("Configuration callback is required");
            }
            customize(api);

            BeforeLoggingActions     = api.Callbacks.BeforeLoggingActions.Any() ? api.Callbacks.BeforeLoggingActions.ToArray() : BeforeLoggingActions;
            LoggingActions           = api.Providers.LoggingActions.Any() ? api.Providers.LoggingActions.Values.ToArray() : LoggingActions;
            CustomNullValue          = api.CustomNullValue ?? CustomNullValue;
            RemoveNewLines           = api.RemoveNewlines ?? RemoveNewLines;
            DeprioritizedValueLength = api.DeprioritizedValueLength ?? DeprioritizedValueLength;
            FieldNameLookup          = api.Naming.FieldNameLookup ?? FieldNameLookup;
            TimestampNaming          = api.Naming.TimestampNaming ?? TimestampNaming;
            TimestampFormatString    = api.Formatting.TimestampFormatString ?? TimestampFormatString;
            SpecialValueFormatting   = api.Formatting.SpecialValueFormatting ?? SpecialValueFormatting;
        }
    }
}
