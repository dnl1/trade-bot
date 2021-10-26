using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Providers;

namespace TradeBot.Logger.Sinks
{
    public static class TelegramSinkExtensions
    {
        public static void AddTelegram(this LoggerBuilder builder, string botId, string chatId)
        {
            builder.AddSink(new TelegramSink(botId, chatId));
        }
    }
}
