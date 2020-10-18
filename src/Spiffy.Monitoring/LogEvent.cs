using System;
using System.Collections.Generic;

namespace Spiffy.Monitoring
{
    public class LogEvent
    {
        public LogEvent(Level level, DateTime time, string formattedTime, string message, IDictionary<string, string> properties)
        {
            Level = level;
            Time = time;
            FormattedTime = formattedTime;
            Message = message;
            Properties = properties;
        }

        public Level Level { get; }
        public DateTime Time { get; }
        public string FormattedTime { get; }
        public string Message { get; }
        
        public string MessageWithTime => $"{FormattedTime} {Message}";
        public IDictionary<string, string> Properties { get; }
    }
}
