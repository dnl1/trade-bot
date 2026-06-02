using System;
using TradeBot.Logger;

namespace TradeBot
{
    internal class ConsoleSink : ILogEventSink
    {
        private static readonly object _lock = new();

        public void Emit(LogEvent evt)
        {
            var (color, label) = evt.Level switch
            {
                LogLevel.Debug => (ConsoleColor.DarkGray, "DEBUG"),
                LogLevel.Info  => (ConsoleColor.Cyan,     "INFO "),
                LogLevel.Warn  => (ConsoleColor.Yellow,   "WARN "),
                LogLevel.Error => (ConsoleColor.Red,      "ERROR"),
                _              => (ConsoleColor.Gray,     "?????")
            };

            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{evt.Timestamp:yyyy-MM-dd HH:mm:ss} UTC ");
                Console.ForegroundColor = color;
                Console.Write($"[{label}] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(evt.Message);
                Console.ResetColor();
            }
        }
    }
}
