using System;
using System.Collections.Generic;

namespace Spiffy.Monitoring
{
    public class TimerCollection
    {
        readonly object _lock = new object();
        readonly Dictionary<string, AutoTimer> _timers = new Dictionary<string, AutoTimer>();

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

        internal void WriteTimerValues(Dictionary<string, string> target, string timeElapsedField)
        {
            lock (_lock)
            {
                if (_timers.Count == 0)
                {
                    return;
                }

                var keys = new List<string>(_timers.Keys);
                keys.Sort(StringComparer.Ordinal);

                foreach (var key in keys)
                {
                    var timer = _timers[key];
                    var timeKey = EventContext.NormalizeKey(string.Concat(timeElapsedField, "_", key));
                    target[timeKey] = EventContext.GetTimeFor(timer.ElapsedMilliseconds);
                    if (timer.Count > 1)
                    {
                        target[EventContext.NormalizeKey(string.Concat("Count_", key))] = timer.Count.ToString();
                    }
                }
            }
        }

        AutoTimer GetTimer(string key)
        {
            lock (_lock)
            {
                if (!_timers.TryGetValue(key, out var timer))
                {
                    timer = new AutoTimer();
                    _timers[key] = timer;
                }
                return timer;
            }
        }
    }
}
