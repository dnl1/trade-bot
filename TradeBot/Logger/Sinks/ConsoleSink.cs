using System;
using System.Text;
using TradeBot.Logger;
using TradeBot.Services;

namespace TradeBot
{
    internal class ConsoleSink: ILogEventSink
    {
        private readonly string _loggingService;

        public ConsoleSink(string loggingService = "")
        {
            _loggingService = loggingService;
        }

        public void Emit(LogEvent logEvent)
        {
            switch (logEvent.Level)
            {
                case LogLevel.Debug:
                    Debug(logEvent.Message);
                    break;
                case LogLevel.Error:
                    Error(logEvent.Message);
                    break;
                case LogLevel.Warn:
                    Warn(logEvent.Message);
                    break;
                case LogLevel.Info:
                    Info(logEvent.Message);
                    break;
            }
        }

        public void Debug(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Log(message, LogLevel.Debug);
        }

        public void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Log(message, LogLevel.Error);
        }

        public void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Log(message, LogLevel.Info);
        }

        public void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Log(message, LogLevel.Warn);

        }

        private void Log(string message, LogLevel level, bool doPostLog = true)
        {
            string msg = BuildMessage(message, level.ToString().ToUpper());
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " - " + msg);
            Console.ForegroundColor = ConsoleColor.Gray;

        }

        private string BuildMessage(string message, string level)
        {
            StringBuilder builder = new();

            if (!string.IsNullOrEmpty(_loggingService))
            {
                builder
                    .Append("[")
                    .Append(_loggingService)
                    .Append("] - ");
            }

            return builder.Append(level)
            .Append(" - ")
            .Append(message).ToString();
        }
    }
}
