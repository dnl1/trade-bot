using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Logger
{
    public class LoggerBuilder
    {
        readonly IList<ILogEventSink> _sinks;

        public LoggerBuilder()
        {
            _sinks = new List<ILogEventSink>();
        }

        public void AddSink(ILogEventSink sink)
        {
            _sinks.Add(sink);
        }

        public ILogger CreateLogger()
        {
            return new Logger(_sinks);
        }
    }
}
