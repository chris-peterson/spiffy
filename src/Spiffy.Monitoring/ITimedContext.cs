using System;

namespace Spiffy.Monitoring
{
    /// <summary>
    /// Represents a timed context.  Typically wraps code within a `using` block.
    /// </summary>
    public interface ITimedContext : IDisposable
    {
        double ElapsedMilliseconds { get; }
    }
}