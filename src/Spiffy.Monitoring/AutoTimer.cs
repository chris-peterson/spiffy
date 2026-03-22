using System;
using System.Diagnostics;

namespace Spiffy.Monitoring
{
    internal class AutoTimer : ITimedContext
    {
        private long _startTimestamp;
        private double _accumulatedMs;
        private bool _running;

        public int Count { get; private set;}

        public AutoTimer()
        {
            Start();
        }

        public double ElapsedMilliseconds
        {
            get
            {
                var ms = _accumulatedMs;
                if (_running)
                {
                    ms += GetElapsedMs(_startTimestamp);
                }
                return ms;
            }
        }

        public void Dispose()
        {
            if (_running)
            {
                _accumulatedMs += GetElapsedMs(_startTimestamp);
                _running = false;
            }
        }

        public void Resume()
        {
            Start();
        }

        public void StartOver()
        {
            _accumulatedMs = 0;
            Count = 0;
            _running = false;
            Start();
        }

        void Start()
        {
            if (_running)
            {
                return;
            }
            Count++;
            _startTimestamp = Stopwatch.GetTimestamp();
            _running = true;
        }

        private static double GetElapsedMs(long startTimestamp)
        {
            return (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
        }
    }
}
