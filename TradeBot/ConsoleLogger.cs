﻿using System;
using System.Text;

namespace TradeBot
{
    internal class ConsoleLogger : ILogger
    {
        private readonly string _loggingService;

        public ConsoleLogger(string loggingService = "")
        {
            _loggingService = loggingService;
        }

        public void Debug(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Log(message, nameof(Debug));
        }

        public void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Log(message, nameof(Error));
        }

        public void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Log(message, nameof(Info));
        }

        public void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Log(message, nameof(Warn));

        }

        private void Log(string message, string level)
        {
            string msg = BuildMessage(message, level.ToUpper());
            Console.WriteLine(msg);
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
