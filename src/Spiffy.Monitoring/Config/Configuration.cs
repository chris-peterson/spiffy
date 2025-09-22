using System;
using System.Linq;
using Spiffy.Monitoring.Config;
using Spiffy.Monitoring.Config.Formatting;
using Spiffy.Monitoring.Config.Naming;

// ReSharper disable once CheckNamespace -- this is intentional to avoid additional nesting
namespace Spiffy.Monitoring
{
    public class Configuration
    {
        internal static Configuration Default { get; set; } = new Configuration();

        internal Action<EventContext>[] BeforeLoggingActions { get; set; }   = [];
        internal Action<LogEvent>[] LoggingActions { get; set; }             = [];
        internal string CustomNullValue { get; set; }                        = null;
        internal bool RemoveNewLines { get; set; }                           = false;
        internal int DeprioritizedValueLength { get; set; }                  = 1024;
        internal IFieldNameLookup FieldNameLookup { get; set; }              = new LegacyFieldNameLookup();
        internal TimestampNaming TimestampNaming { get; set; }               = TimestampNaming.UseUnnamedFieldInBrackets;
        internal string TimestampFormatString { get; set; }                  = "yyyy-MM-dd HH:mm:ss.fffK";
        internal SpecialValueFormatting SpecialValueFormatting { get; set; } = SpecialValueFormatting.UseAlternateQuotes;

        public static Configuration Initialize(Action<InitializationApi> customize)
        {
            Configuration c = new Configuration();
            var api = new InitializationApi();
            if (customize == null)
            {
                throw new Exception("Configuration callback is required");
            }
            customize(api);
            c.BeforeLoggingActions = api.Callbacks.BeforeLoggingActions.Any() ? api.Callbacks.BeforeLoggingActions.ToArray() : c.BeforeLoggingActions;
            c.LoggingActions = api.Providers.LoggingActions.Any() ? api.Providers.LoggingActions.Values.ToArray() : c.LoggingActions;
            c.CustomNullValue = api.CustomNullValue ?? c.CustomNullValue;
            c.RemoveNewLines = api.RemoveNewlines ?? c.RemoveNewLines;
            c.DeprioritizedValueLength = api.DeprioritizedValueLength ?? c.DeprioritizedValueLength;
            c.FieldNameLookup = api.Naming.FieldNameLookup ?? c.FieldNameLookup;
            c.TimestampNaming = api.Naming.TimestampNaming ?? c.TimestampNaming;
            c.TimestampFormatString = api.Formatting.TimestampFormatString ?? c.TimestampFormatString;
            c.SpecialValueFormatting = api.Formatting.SpecialValueFormatting ?? c.SpecialValueFormatting;

            return Default = c;
        }
    }
}
