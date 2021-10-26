using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot;
using TradeBot.Logger;
using TradeBot.Services;
using TradeBot.Settings;

namespace TradeBot
{
    public class TelegramSink : ILogEventSink
    {
        private readonly ITelegramBotClient _client;
        ConcurrentQueue<LogEvent> _events;
        private readonly int _chatId;

        public TelegramSink(string botId, string chatId)
        {
            _client = new TelegramBotClient(botId ?? throw new ArgumentNullException(nameof(botId)));
            _chatId = int.Parse(chatId ?? throw new ArgumentNullException(nameof(chatId)));
            _events = new ConcurrentQueue<LogEvent>();

            var thread = new Thread(async () => await QueueProcessor());
            thread.Start();
        }

        public void Emit(LogEvent logEvent)
        {
            _events.Enqueue(logEvent);
        }

        private async Task QueueProcessor()
        {
            while (true)
            {
                if (_events.TryDequeue(out LogEvent logEvent))
                    await SendMessage(logEvent.Message);
            }
        }

        private async Task SendMessage(string msg)
        {
            if (null != _client)
                await _client.SendTextMessageAsync(new Telegram.Bot.Types.ChatId(_chatId), msg);
        }
    }
}
