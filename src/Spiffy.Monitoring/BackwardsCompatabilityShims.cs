// this file exists to preserve previous API artifacts (version 5.2.0 and earlier)
// (https://github.com/chris-peterson/spiffy/blob/8ec62fde0a8658b8c511ecb2d4f3a9b393dd2988/src/Spiffy.Monitoring/ExtensionMethods.cs)
// if these extensions were to be removed, applications built against earlier versions
// would experience runtime errors, e.g.:
// System.TypeLoadException: Could not load type 'Spiffy.Monitoring.ExceptionExtensions' from
// assembly 'Spiffy.Monitoring, Version=6.0.0.0, Culture=neutral, PublicKeyToken=null'.
//
// given there are likely many library assemblies (as opposed to edge assemblies) that
// couple to this library, requiring them all to re-compile is untenable

using System;

namespace Spiffy.Monitoring
{
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
        public static EventContext IncludeStructure(this EventContext eventContext, object structure, string keyPrefix = null, bool includeNullValues = true)
        {
            return eventContext.IncludeStructure(structure, keyPrefix, includeNullValues);
        }
    }
}