using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Spiffy.Monitoring
{
    public class CompositeEventContext : EventContext
    {
        public CompositeEventContext(string component, string operation, IDictionary<string, ILoggingFacade> loggerCollection) : base(component, operation)
        {
            if(null == loggerCollection)
            {
                LoggerCollection.Add("default", DefaultLoggingFacade.Instance);
            }
            else
            {
                LoggerCollection = new Dictionary<string, ILoggingFacade>(loggerCollection);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public CompositeEventContext(IDictionary<string, ILoggingFacade> loggerCollection = null) : base()
        {
            if(null == loggerCollection)
            {
                LoggerCollection.Add("default", DefaultLoggingFacade.Instance);
            }
            else
            {
                LoggerCollection = new Dictionary<string, ILoggingFacade>(loggerCollection);
            }
        }

        public IDictionary<string, ILoggingFacade> LoggerCollection { get; protected set; } = new Dictionary<string, ILoggingFacade>();

        protected override void Dispose(bool disposing)
        {
            if( !_disposed)
            {
                this["TimeElapsed"] = GetTimeFor(_timer.TotalMilliseconds);
                foreach(var logger in LoggerCollection) {
                    logger.Value.Log(Level, GetFormattedMessage(logger.Key));
                }

                if(disposing)
                {
                    LoggerCollection = null;
                }

                _disposed = true;
            }
        }
    }
}