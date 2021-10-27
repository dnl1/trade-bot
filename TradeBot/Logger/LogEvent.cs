using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Logger
{
    public class LogEvent
    {
        public LogEvent(LogLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        public LogLevel Level { get; private set; }
        public string Message { get; private set; }
    }
}
