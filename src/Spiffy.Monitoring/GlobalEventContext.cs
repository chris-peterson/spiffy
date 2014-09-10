using System.Collections.Generic;

namespace Spiffy.Monitoring
{
    public class GlobalEventContext
    {
        private GlobalEventContext()
        {
        }

        static GlobalEventContext _instance;

        public static GlobalEventContext Instance
        {
            get { return _instance ?? (_instance = new GlobalEventContext()); }
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