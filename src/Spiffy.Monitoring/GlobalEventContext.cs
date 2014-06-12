using System;
using System.Collections.Generic;

namespace Spiffy.Monitoring
{
    public class GlobalEventContext
    {
        private GlobalEventContext()
        {
        }

        private static readonly Lazy<GlobalEventContext> _instance = new Lazy<GlobalEventContext>(() => new GlobalEventContext());

        public static GlobalEventContext Instance
        {
            get { return _instance.Value; }
        }

        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public GlobalEventContext Set(string key, object value)
        {
            _values[key] = value;
            return this;
        }

        internal void CopyTo(EventContext other)
        {
            foreach (var kvp in _values)
            {
                other[kvp.Key] = kvp.Value;
            }
        }
    }
}