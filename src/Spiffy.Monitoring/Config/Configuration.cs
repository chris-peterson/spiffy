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

        // Defaults are listed below.
        // NOTE: this is intentionally flattened representation of the public-facing APIs (anti-corruption layer)
        // as well as keeping all the defaults together
        // Callbacks:
        internal Action<EventContext>[] BeforeLoggingActions { get; set; } = [];
        // Formatting:
        internal string TimestampFormatString { get; set; }                  = "yyyy-MM-dd HH:mm:ss.fffK";
        internal SpecialValueFormatting SpecialValueFormatting { get; set; } = SpecialValueFormatting.UseAlternateQuotes;
        internal NewlineFormatting NewlineFormatting { get; set; }           = NewlineFormatting.Preserve;
        internal string CustomNullValue { get; set; }                        = null;
        internal int DeprioritizedValueLength { get; set; }                  = 1024;
        // Naming:
        internal IFieldNameLookup FieldNameLookup { get; set; } = new LegacyFieldNameLookup();
        internal TimestampNaming TimestampNaming { get; set; }  = TimestampNaming.UseUnnamedFieldInBrackets;
        // Providers:
        internal Action<LogEvent>[] LoggingActions { get; set; } = [];

        public static void Initialize(Action<InitializationApi> customize)
        {
            Default = Create(customize);
        }

        internal static Configuration Create(Action<InitializationApi> customize)
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
            if (api.Formatting.CustomNullValue != null)
            {
                c.CustomNullValue = api.Formatting.CustomNullValue;
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete
                c.CustomNullValue = api.CustomNullValue;
#pragma warning restore CS0618 // Type or member is obsolete
            }
            
            if (api.Formatting.NewlineFormatting.HasValue)
            {
                c.NewlineFormatting = api.Formatting.NewlineFormatting.Value;
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (api.RemoveNewlines)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    c.NewlineFormatting = NewlineFormatting.Remove;
                }
            }
            if (api.Formatting.DeprioritizedValueLength.HasValue)
            {
                c.DeprioritizedValueLength = api.Formatting.DeprioritizedValueLength.Value;
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (api.DeprioritizedValueLength != -1)
                {
                    c.DeprioritizedValueLength = api.DeprioritizedValueLength;
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
            c.FieldNameLookup = api.Naming.FieldNameLookup ?? c.FieldNameLookup;
            c.TimestampNaming = api.Naming.TimestampNaming ?? c.TimestampNaming;
            c.TimestampFormatString = api.Formatting.TimestampFormatString ?? c.TimestampFormatString;
            c.SpecialValueFormatting = api.Formatting.SpecialValueFormatting ?? c.SpecialValueFormatting;
            return c;
        }
    }
}
