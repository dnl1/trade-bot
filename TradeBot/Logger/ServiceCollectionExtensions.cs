using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Logger.Sinks;
using TradeBot.Settings;

namespace TradeBot.Logger
{
    public static class ServiceCollectionExtensions
    {
        const string CONSOLE_LOGGER = "console";
        const string TELEGRAM_LOGGER = "telegram";
        public static IServiceCollection AddLoggers(this IServiceCollection services, AppSettings appSettings)
        {
            var loggerBuilder = new LoggerBuilder();

            if(Array.Exists(appSettings.Loggers, logger => logger.Equals(CONSOLE_LOGGER))) 
            {
                loggerBuilder.AddConsole();
            }

            if (Array.Exists(appSettings.Loggers, logger => logger.Equals(TELEGRAM_LOGGER)) && 
                !string.IsNullOrEmpty(appSettings.TelegramBotId) &&
                !string.IsNullOrEmpty(appSettings.TelegramChatId))
            {
                loggerBuilder.AddTelegram(appSettings.TelegramBotId, appSettings.TelegramChatId);
            }

            var logger = loggerBuilder.CreateLogger();

            services.AddSingleton(logger);

            return services;
        }
    }
}
