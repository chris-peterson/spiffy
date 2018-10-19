using System;
using System.Diagnostics;

namespace Spiffy.Monitoring
{
    internal class AutoTimer : IDisposable
    {
        readonly Stopwatch _stopwatch = new Stopwatch();

        public int Count { get; private set;}

        public AutoTimer()
        {
            Start();
        }

        public double TotalMilliseconds => _stopwatch.Elapsed.TotalMilliseconds;

        public void Dispose()
        {
            _stopwatch.Stop();
        }

        public void Resume()
        {
            Start();
        }

        void Start()
        {
            Count++;
            _stopwatch.Start();
        }
    }
}