using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Spiffy.Monitoring
{
    public class TimerCollection
    {
        readonly ConcurrentDictionary<string, AutoTimer> _timers = new ConcurrentDictionary<string, AutoTimer>();
        
        public ITimedContext TimeOnce(string key)
        {
            var timer = GetTimer(key);
            timer.StartOver();
            return timer;
        }

        public ITimedContext Accumulate(string key)
        {
            var timer = GetTimer(key);
            timer.Resume();
            return timer;
        }
        internal Dictionary<string, AutoTimer> ShallowClone() => new Dictionary<string, AutoTimer>(_timers);

        AutoTimer GetTimer(string key)
        {
            return _timers.GetOrAdd(key, _ => new AutoTimer());
        }
    }
}
