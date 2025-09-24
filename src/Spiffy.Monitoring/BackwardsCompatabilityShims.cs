// this file exists to preserve previous API artifacts

using System;

namespace Spiffy.Monitoring
{
    // (https://github.com/chris-peterson/spiffy/blob/8ec62fde0a8658b8c511ecb2d4f3a9b393dd2988/src/Spiffy.Monitoring/ExtensionMethods.cs)
    // if these extensions were to be removed, applications built against earlier versions
    // would experience runtime errors, e.g.:
    // System.TypeLoadException: Could not load type 'Spiffy.Monitoring.ExceptionExtensions' from
    // assembly 'Spiffy.Monitoring, Version=6.0.0.0, Culture=neutral, PublicKeyToken=null'.
    //
    // given there are likely many library assemblies (as opposed to edge assemblies) that
    // couple to this library, requiring them all to re-compile is untenable
    public static class ExceptionExtensions
    {
        [Obsolete("This extension should be avoided, instead preferring IncludeException on EventContext")]
        public static void IncludeException(this EventContext target, Exception ex, string keyPrefix = "Exception")
        {
            target.IncludeException(ex, keyPrefix);
        }

        [Obsolete("This extension should be avoided, instead preferring IncludeInformationalException on EventContext")]
        public static void IncludeInformationalException(this EventContext target, Exception ex, string keyPrefix)
        {
            target.IncludeInformationalException(ex, keyPrefix);
        }
    }

    public static class EventContextExtensions
    {
        [Obsolete("This extension should be avoided, instead preferring IncludeStructure on EventContext")]
        public static EventContext IncludeStructure(this EventContext eventContext, object structure, string keyPrefix = null, bool includeNullValues = false)
        {
            return eventContext.IncludeStructure(structure, keyPrefix, includeNullValues);
        }
    }
}

namespace Spiffy.Monitoring.Config
{
    // Preserving these public constructors
    // I don't expect anyone to have used them as there wouldn't be much point,
    // but keeping to preserve full API compatibility until 7.x
    public partial class InitializationApi
    {
        /// <summary>
        /// If set, this value is used for logging values that are null.
        /// </summary>
        [Obsolete("superseded by Formatting.NullValue")]
        public string CustomNullValue { get; set; }

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

        public partial class CallbacksApi
        {
            [Obsolete("This class is not intended for public use.  It will go away in future versions")]
            public CallbacksApi(InitializationApi parent)
            {
            }
        }
        public partial class FormattingApi
        {
            [Obsolete("This class is not intended for public use.  It will go away in future versions")]
            public FormattingApi(InitializationApi parent)
            {
            }
        }
        public partial class NamingApi
        {
            [Obsolete("This class is not intended for public use.  It will go away in future versions")]
            public NamingApi(InitializationApi parent)
            {
            }


            [Obsolete("superseded by ShortFieldNames")]
            public void UseShortFieldNames()
            {
                ShortFieldNames();
            }
        }
        public partial class ProvidersApi
        {
            [Obsolete("This class is not intended for public use.  It will go away in future versions")]
            public ProvidersApi(InitializationApi parent)
            {
            }
        }
    }
}
