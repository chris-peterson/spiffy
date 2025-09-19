using System;
using System.Collections.Generic;
using Spiffy.Monitoring.Config.Formatting;
using Spiffy.Monitoring.Config.Naming;

namespace Spiffy.Monitoring.Config
{
    public class InitializationApi
    {
        // Custom providers should extend this API by way of extension methods
        public class ProvidersApi()
        {
            internal readonly Dictionary<string, Action<LogEvent>> LoggingActions = new();
            public void Add(string id, Action<LogEvent> loggingAction)
            {
                LoggingActions[id] = loggingAction;
            }
        }

        public class CallbacksApi()
        {
            internal readonly List<Action<EventContext>> BeforeLoggingActions = new();
            public void BeforeLogging(Action<EventContext> action)
            {
                BeforeLoggingActions.Add(action);
            }
        }

        public class NamingApi()
        {
            public NamingApi UseShortFieldNames()
            {
                FieldNameLookup = new ShortFieldNameLookup();
                return this;
            }

            public NamingApi Timestamp(TimestampNaming behavior)
            {
                TimestampNaming = behavior;
                return this;
            }

            internal IFieldNameLookup FieldNameLookup { get; private set;}
            internal TimestampNaming? TimestampNaming { get; private set; }
        }

        public class FormattingApi()
        {
            public FormattingApi SpecialValue(SpecialValueFormatting formatting)
            {
                SpecialValueFormatting = formatting;
                return this;
            }

            internal SpecialValueFormatting? SpecialValueFormatting { get; private set; }

            public FormattingApi Timestamp(string formatString)
            {
                TimestampFormatString = formatString;
                return this;
            }
            internal string TimestampFormatString { get; private set; }
        }

        public ProvidersApi Providers { get; }
        public CallbacksApi Callbacks { get; }

        public InitializationApi()
        {
            Providers  = new ProvidersApi();
            Callbacks  = new CallbacksApi();
            Naming     = new NamingApi();
            Formatting = new FormattingApi();
        }

        /// <summary>
        /// If set, this value is used for logging values that are null.
        /// </summary>
        public string CustomNullValue { get; set; }

        public NamingApi Naming { get; }

        public FormattingApi Formatting { get; }

        /// <summary>
        /// Whether to remove newline characters from logged values.
        /// </summary>
        /// <returns>
        /// <code>true</code> if newline characters will be removed from logged
        /// values, <code>false</code> otherwise.
        /// </returns>
        public bool? RemoveNewlines { get; set; }

        /// <summary>
        /// Values over this length will be deprioritized in the <see cref="LogEvent.Message"/>.
        /// Defaults to 1024.
        /// </summary>
        /// <remarks>
        /// In some logging scenarios, long values can result in some key/value pairs being cut off.
        /// Key/value pairs with values whose length exceeds this value will be output after those
        /// pairs whose values do not.
        /// </remarks>
        public int? DeprioritizedValueLength { get; set; }

        public InitializationApi UseLogfmt()
        {
            Naming.Timestamp(TimestampNaming.UseTimestampField);
            Formatting.Timestamp("yyyy-MM-ddTHH:mm:ss.fffZ");
            Formatting.SpecialValue(SpecialValueFormatting.UseAndEscapeDoubleQuotes);
            RemoveNewlines = true;
            return this;
        }
    }
}
