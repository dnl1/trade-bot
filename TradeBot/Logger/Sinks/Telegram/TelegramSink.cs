using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using TradeBot.Logger;
using System.Net.Http;

namespace TradeBot
{
    public class TelegramSink : ILogEventSink, IDisposable
    {
        private readonly LogLevel _minLevel = LogLevel.Info;
        private readonly ITelegramBotClient _client;
        private readonly ConcurrentQueue<LogEvent> _events = new();
        private readonly int _chatId;
        private readonly CancellationTokenSource _cts = new();

        public TelegramSink(string botId, string chatId, HttpClient httpClient)
        {
            _client  = new TelegramBotClient(botId  ?? throw new ArgumentNullException(nameof(botId)),  httpClient);
            _chatId  = int.Parse(chatId ?? throw new ArgumentNullException(nameof(chatId)));
            _ = Task.Run(() => QueueProcessor(_cts.Token));
        }

        public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);

        private async Task QueueProcessor(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (_events.TryDequeue(out var evt) && evt.Level >= _minLevel)
                    {
                        try { await SendMessage(Format(evt), ct); }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[TelegramSink] Failed to send: {ex.Message}");
                        }
                    }
                    else
                    {
                        await Task.Delay(100, ct);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[TelegramSink] Queue error: {ex.Message}");
                }
            }
        }

        private static string Format(LogEvent evt)
        {
            var emoji = evt.Level switch
            {
                LogLevel.Info  => "ℹ️",
                LogLevel.Warn  => "⚠️",
                LogLevel.Error => "❌",
                _              => "📋"
            };
            return $"{emoji} `{evt.Timestamp:HH:mm:ss} UTC`\n{evt.Message}";
        }

        private async Task SendMessage(string msg, CancellationToken ct) =>
            await _client.SendTextMessageAsync(
                new Telegram.Bot.Types.ChatId(_chatId), msg,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: ct);

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
