using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Logger.Sinks
{
    public static class ConsoleSinkExtensions
    {
        public static void AddConsoleLogger(this LoggerBuilder builder)
        {
            builder.AddSink(new ConsoleSink("tradebot-logger"));
        }
    }
}
