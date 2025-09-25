using System;
using System.Collections.Generic;
using Spiffy.Monitoring.Config.Formatting;
using Spiffy.Monitoring.Config.Naming;

namespace Spiffy.Monitoring.Config
{
    public partial class InitializationApi
    {
        // Custom providers should extend this API by way of extension methods

        public partial class ProvidersApi
        {
            // TODO: this should be internal
            public ProvidersApi()
            {
            }

            internal readonly Dictionary<string, Action<LogEvent>> LoggingActions = new();
            public void Add(string id, Action<LogEvent> loggingAction)
            {
                LoggingActions[id] = loggingAction;
            }
        }

        public partial class CallbacksApi
        {
            // TODO: this should be internal
            public CallbacksApi()
            {
            }

            internal readonly List<Action<EventContext>> BeforeLoggingActions = new();
            public void BeforeLogging(Action<EventContext> action)
            {
                BeforeLoggingActions.Add(action);
            }
        }

        public partial class NamingApi
        {
            // TODO: this should be internal
            public NamingApi()
            {
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

        public partial class FormattingApi
        {
            // TODO: this should be internal
            public FormattingApi()
            {
            }

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

        public NamingApi Naming { get; }

        public FormattingApi Formatting { get; }

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
