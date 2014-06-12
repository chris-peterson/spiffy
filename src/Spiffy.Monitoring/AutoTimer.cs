using System;
using System.Diagnostics;

namespace Spiffy.Monitoring
{
    public class AutoTimer : IDisposable
    {
        public AutoTimer()
        {
            _stopwatch.Start();
        }

        private readonly Stopwatch _stopwatch = new Stopwatch();

        public double TotalMilliseconds
        {
            get { return _stopwatch.Elapsed.TotalMilliseconds; }
        }

        public void Dispose()
        {
            _stopwatch.Stop();
        }
    }
}