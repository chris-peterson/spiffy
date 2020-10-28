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
                this[keyPrefix + "_Type"] = ex.GetType().Name;
                this[keyPrefix + "_Message"] = ex.Message;
                this[keyPrefix + "_StackTrace"] = ex.StackTrace;

                // And retain the innermost exception, if any
                var inner = ex.InnerException;
                var innerPrefix = string.Format("Innermost{0}", keyPrefix);
                while (inner != null)
                {
                    if (inner.InnerException == null)
                    {
                        this[innerPrefix + "_Type"] = inner.GetType().Name;
                        this[innerPrefix + "_Message"] = inner.Message;
                        this[innerPrefix + "_StackTrace"] = inner.StackTrace;
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
                this[keyPrefix] =
                    $"See Exception_*{(inner == null ? null : $" and {innerPrefix}_*")} for more details";
            }
        }
    }
}
