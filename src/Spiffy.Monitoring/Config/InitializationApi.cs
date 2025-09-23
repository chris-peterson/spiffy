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
            [Obsolete("superseded by ShortFieldNames")]
            public void UseShortFieldNames()
            {
                ShortFieldNames();
            }

            public NamingApi ShortFieldNames()
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

            public FormattingApi Newlines(NewlineFormatting formatting)
            {
                NewlineFormatting = formatting;
                return this;
            }
            internal NewlineFormatting? NewlineFormatting { get; private set; }

            public FormattingApi NullValue(string nullValue)
            {
                CustomNullValue = nullValue;
                return this;
            }
            internal string CustomNullValue { get; private set; }

            public FormattingApi DeprioritizeValueLength(int length)
            {
                DeprioritizedValueLength = length;
                return this;
            }
            internal int? DeprioritizedValueLength { get; private set; }
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
        [Obsolete("superseded by Formatting.NullValue")]
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
        [Obsolete("superseded by Formatting.Newlines")]
        public bool RemoveNewlines { get; set; } = false;

        /// <summary>
        /// Values over this length will be deprioritized in the <see cref="LogEvent.Message"/>.
        /// Defaults to 1024.
        /// </summary>
        /// <remarks>
        /// In some logging scenarios, long values can result in some key/value pairs being cut off.
        /// Key/value pairs with values whose length exceeds this value will be output after those
        /// pairs whose values do not.
        /// </remarks>
        [Obsolete("superseded by Formatting.DeprioritizeValueLength")]
        public int DeprioritizedValueLength { get; set; } = -1;

        public InitializationApi UseLogfmt()
        {
            Formatting.Newlines(NewlineFormatting.Remove);
            Formatting.SpecialValue(SpecialValueFormatting.UseAndEscapeDoubleQuotes);
            Formatting.Timestamp("yyyy-MM-ddTHH:mm:ss.fffZ");
            Naming.Timestamp(TimestampNaming.UseTimestampField);
            return this;
        }
    }
}
