using System;
using System.Diagnostics;

namespace Spiffy.Monitoring
{
    internal class AutoTimer : ITimedContext
    {
        readonly Stopwatch _stopwatch = new Stopwatch();

        public int Count { get; private set;}

        public AutoTimer()
        {
            Start();
        }

        public double ElapsedMilliseconds => _stopwatch.Elapsed.TotalMilliseconds;
        public bool IsRunning => _stopwatch.IsRunning;

        public void Dispose()
        {
            _stopwatch.Stop();
        }

        public void Resume()
        {
            Start();
        }

        public void StartOver()
        {
            _stopwatch.Reset();
            Count = 0;
            Start();
        }

        void Start()
        {
            Count++;
            _stopwatch.Start();
        }
    }
}