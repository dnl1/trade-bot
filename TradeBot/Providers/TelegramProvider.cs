using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using TradeBot.Services;
using TradeBot.Settings;

namespace TradeBot.Providers
{
    internal class TelegramProvider : INotificationProvider
    {
        private readonly ITelegramBotClient _client;
        private readonly int _identifier;

        public TelegramProvider(AppSettings appSettings)
        {
            string[] splited = appSettings.TelegramBotId.Split('/');

            _client = new TelegramBotClient(splited[2]);

            _identifier = int.Parse(splited[3]);
        }

        public async Task SendMessage(string msg)
        {
            await _client.SendTextMessageAsync(new Telegram.Bot.Types.ChatId(_identifier), msg);
        }
    }
}
