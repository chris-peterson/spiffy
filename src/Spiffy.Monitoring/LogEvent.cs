using System;
using System.Collections.Generic;

namespace Spiffy.Monitoring
{
    public class LogEvent
    {
        public LogEvent(Level level, DateTime timestamp, TimeSpan timeElapsed, string formattedTime, string message, IDictionary<string, string> properties, IDictionary<string, string> privateData)
        {
            Level = level;
            Timestamp = timestamp;
            TimeElapsed = timeElapsed;
            FormattedTime = formattedTime;
            Message = message;
            Properties = properties;
            PrivateData = privateData;
        }

        public Level Level { get; }
        public DateTime Timestamp { get; }
        public TimeSpan TimeElapsed { get; }
        public string FormattedTime { get; }
        public string Message { get; }
        
        public string MessageWithTime => $"{FormattedTime} {Message}";
        public IDictionary<string, string> Properties { get; }
        public IDictionary<string, string> PrivateData { get; }
    }
}
