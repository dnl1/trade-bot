using System;

namespace TradeBot.Logger
{
    public class LogEvent
    {
        public LogEvent(LogLevel level, string message)
        {
            Level     = level;
            Message   = message;
            Timestamp = DateTime.UtcNow;
        }

        public LogLevel  Level     { get; }
        public string    Message   { get; }
        public DateTime  Timestamp { get; }
    }
}
