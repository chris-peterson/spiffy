using System;

namespace Spiffy.Monitoring
{
    partial class EventContext
    {
        public EventContext IncludeException(Exception ex, string keyPrefix = "Exception")
        {
            SetToError("An exception has occurred");
            IncludeExceptionHelper(ex, keyPrefix);
            return this;
        }

        public EventContext IncludeInformationalException(Exception ex, string keyPrefix)
        {
            IncludeExceptionHelper(ex, keyPrefix);
            return this;
        }

        void IncludeExceptionHelper(Exception ex, string keyPrefix)
        {
            if (ex == null)
            {
                // we don't expect to be called with a null exception, but we should emit something:
                this[keyPrefix] = null;
            }
            else
            {
                // break out into keys that are easier to search for
                this[string.Concat(keyPrefix, "_Type")] = ex.GetType().Name;
                this[string.Concat(keyPrefix, "_Message")] = ex.Message;
                this[string.Concat(keyPrefix, "_StackTrace")] = StackTraceCleanup.Simplify(ex.StackTrace);

                // And retain the innermost exception, if any
                var inner = ex.InnerException;
                var innerPrefix = string.Concat("Innermost", keyPrefix);
                while (inner != null)
                {
                    if (inner.InnerException == null)
                    {
                        this[string.Concat(innerPrefix, "_Type")] = inner.GetType().Name;
                        this[string.Concat(innerPrefix, "_Message")] = inner.Message;
                        this[string.Concat(innerPrefix, "_StackTrace")] = StackTraceCleanup.Simplify(inner.StackTrace);
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
                this[keyPrefix] = inner == null
                    ? "See Exception_* for more details"
                    : string.Concat("See Exception_* and ", innerPrefix, "_* for more details");
            }
        }
    }
}
