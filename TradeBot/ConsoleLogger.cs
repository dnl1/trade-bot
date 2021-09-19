using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            string msg = BuildMessage(message, "DEBUG");
            Console.WriteLine(msg);
        }

        private string BuildMessage(string message, string type)
        {
            StringBuilder builder = new();

            if (!string.IsNullOrEmpty(_loggingService))
            {
                builder
                    .Append("[")
                    .Append(_loggingService)
                    .Append("] - ");
            }

            return builder.Append(type)
            .Append(" - ")
            .Append(message).ToString();
        }

        public void Error(string message)
        {
            string msg = BuildMessage(message, "ERROR");
            Console.WriteLine(msg);
        }

        public void Information(string message)
        {
            string msg = BuildMessage(message, "INFO");
            Console.WriteLine(msg);
        }

        public void Warning(string message)
        {
            string msg = BuildMessage(message, "WARN");
            Console.WriteLine(msg);
        }
    }
}
