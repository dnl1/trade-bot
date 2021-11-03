using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Logger
{
    internal class Logger : ILogger
    {
        private readonly IEnumerable<ILogEventSink> _sinks;

        public Logger(IEnumerable<ILogEventSink> sinks)
        {
            _sinks = sinks ?? throw new InvalidOperationException("Needs at least a sink to continue");
        }

        public void Debug(string message)
        {
            var logEvent = new LogEvent(LogLevel.Debug, message);
            Emit(logEvent);
        }

        public void Error(string message)
        {
            var logEvent = new LogEvent(LogLevel.Error, message);
            Emit(logEvent);
        }

        public void Info(string message)
        {
            var logEvent = new LogEvent(LogLevel.Info, message);
            Emit(logEvent);
        }

        public void Warn(string message)
        {
            var logEvent = new LogEvent(LogLevel.Warn, message);
            Emit(logEvent);
        }

        private void Emit(LogEvent logEvent)
        {
            foreach (var sink in _sinks)
            {
                sink.Emit(logEvent);
            }
        }
    }
}
