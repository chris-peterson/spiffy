using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Spiffy.Monitoring
{
    public static class ExceptionExtensions
    {
        public static void IncludeException(this EventContext target, Exception ex, string keyPrefix = "Exception")
        {
            target.SetToError("An exception has ocurred");
            IncludeExceptionHelper(target, ex, keyPrefix);       
        }

        public static void IncludeInformationalException(this EventContext target, Exception ex, string keyPrefix)
        {
            IncludeExceptionHelper(target, ex, keyPrefix);      
        }


        static void IncludeExceptionHelper(EventContext target, Exception ex, string keyPrefix)
        {
            if (ex == null)
            {
                // we don't expect to be called with a null exception, but we should emit something:
                target[keyPrefix] = null;
            }
            else
            {
                // break out into keys that are easier to search for
                target[keyPrefix + "_Type"] = ex.GetType().Name;
                target[keyPrefix + "_Message"] = ex.Message;
                target[keyPrefix + "_StackTrace"] = ex.StackTrace;

                // And retain the innermost exception, if any
                var inner = ex.InnerException;
                var innerPrefix = string.Format("Innermost{0}", keyPrefix);
                while (inner != null)
                {
                    if (inner.InnerException == null)
                    {
                        target[innerPrefix + "_Type"] = inner.GetType().Name;
                        target[innerPrefix + "_Message"] = inner.Message;
                        target[innerPrefix + "_StackTrace"] = inner.StackTrace;
                        break;
                    }
                    inner = inner.InnerException;
                }

                // NOTE: In addition to the fields emitted above, we used to emit the full ex.ToString()
                // We stopped doing this, because it is redundant, and (more importantly) because it was 
                // causing problems with Splunk indexing.  In certain conditions, fields after Exception 
                // (e.g. Service/BuildLife) were not being indexed.  We attribute this to hiting a max 
                // event limit of 10K.
                // We will continue to emit a value for reporting and discoverability, i.e. 
                // "index=appdev Service=Foo Exception" will still return all events that have an exception.
                target[keyPrefix] = string.Format("See Exception_*{0} for more details",
                    inner == null ? null : string.Format(" and {0}_*", innerPrefix));
            }
        }
    }

    internal static class StringExtensions
    {
        private const string WhitespaceRegexPattern = @"\s+";

        public static bool StartsWithQuote(this string value)
        {
            return string.IsNullOrEmpty(value) == false && value[0] == '"';
        }

        public static bool ContainsWhitespace(this string value)
        {
            return value != null && Regex.IsMatch(value, WhitespaceRegexPattern);
        }

        public static string RemoveWhitespace(this string value)
        {
            return value == null ? null : Regex.Replace(value.Trim(), WhitespaceRegexPattern, "_");
        }

        public static string WrappedInQuotes(this string value)
        {
            return string.Format(@"""{0}""", value);
        }

        public static string WrappedInBrackets(this string value)
        {
            return string.Format("[{0}]", value);
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            return value == null || value.All(char.IsWhiteSpace);
        }
    }

    public static class EventContextExtensions
    {
        public static EventContext IncludeStructure(this EventContext eventContext, object structure, string keyPrefix = null, bool includeNullValues = true)
        {
            if (structure != null)
            {
#if NET4_0
                foreach (var property in structure.GetType().GetProperties().Where(p => p.CanRead))
#else
                foreach (var property in structure.GetType().GetTypeInfo().DeclaredProperties.Where(p => p.CanRead))
#endif
                {
                    try
                    {
                        var val = property.GetValue(structure, null);
                        if (val == null && !includeNullValues)
                        {
                            continue;
                        }
                        string key = string.IsNullOrEmpty(keyPrefix)
                            ? property.Name
                            : string.Format("{0}_{1}", keyPrefix, property.Name);
                        eventContext[key] = val;
                    }
                    catch // intentionally squashed
                    {
                    }
                }
            }

            return eventContext;
        }
    }
}