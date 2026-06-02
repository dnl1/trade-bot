using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using TradeBot.Logger.Sinks;
using TradeBot.Settings;

namespace TradeBot.Logger
{
    public static class ServiceCollectionExtensions
    {
        private const string ConsoleKey  = "console";
        private const string TelegramKey = "telegram";
        private const string FileKey     = "file";

        public static IServiceCollection AddLoggers(this IServiceCollection services, AppSettings appSettings)
        {
            services.AddSingleton(sp =>
            {
                var builder = new LoggerBuilder();

                if (Has(appSettings, ConsoleKey))
                    builder.AddConsole();

                if (Has(appSettings, FileKey))
                    builder.AddFile(appSettings.LogFilePath);

                if (Has(appSettings, TelegramKey)
                    && !string.IsNullOrEmpty(appSettings.TelegramBotId)
                    && !string.IsNullOrEmpty(appSettings.TelegramChatId))
                {
                    var factory = sp.GetService<IHttpClientFactory>();
                    if (factory is not null)
                        builder.AddTelegram(appSettings.TelegramBotId, appSettings.TelegramChatId,
                            factory.CreateClient("telegram"));
                }

                return builder.CreateLogger();
            });

            return services;
        }

        private static bool Has(AppSettings s, string key) =>
            Array.Exists(s.Loggers, l => l.Equals(key, StringComparison.OrdinalIgnoreCase));
    }
}
