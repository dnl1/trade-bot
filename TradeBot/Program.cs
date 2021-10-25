using FluentScheduler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using TradeBot;
using TradeBot.Database;
using TradeBot.Factories;
using TradeBot.HostedServices;
using TradeBot.Providers;
using TradeBot.Repositories;
using TradeBot.Services;
using TradeBot.Settings;
using TradeBot.Strategies;
using System.Reflection;

public class Program
{
    private static IConfiguration? Configuration = null;

    public static void Main(string[] args)
    {
        Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(builder =>
            {
                builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables();

                Configuration = builder.Build();
            })
            .ConfigureServices(services =>
            {
                services.AddHostedService<TradingService>();

                var appSettings = new AppSettings();
                Configuration.Bind(appSettings);

                JobManager.Initialize();

                services.AddSingleton(appSettings);
                services.AddSingleton<BinanceApiManager>();
                services.AddSingleton<BinanceStreamManager>();
                services.AddSingleton<BinanceApiClient>();
                services.AddSingleton<MarketDataListenerService>();
                services.AddSingleton<StrategyFactory>();
                services.AddSingleton<DefaultStrategy>();
                services.AddSingleton<MultipleCoinsStrategy>();
                services.AddSingleton<ILogger>(sp => new ConsoleLogger((msg) => sp.GetService<INotificationService>()?.Notify(msg), "tradebot-logger"));
                services.AddSingleton(typeof(IDatabase<>), typeof(InMemoryDatabase<>));
                services.AddSingleton<ICacher, Cacher>();
                services.AddSingleton<ISnapshotRepository, SnapshotRepository>();
                services.AddSingleton<IPairRepository, PairRepository>();
                services.AddSingleton<ICoinRepository, CoinRepository>();
                services.AddSingleton<ITradeRepository, TradeRepository>();
                services.AddSingleton<ITradeService, TradeService>();

                services.AddSingleton<TelegramProvider>();
                services.AddSingleton(sp =>
                {
                    var types = Assembly.GetExecutingAssembly().GetTypes().Where(p => typeof(INotificationProvider).IsAssignableFrom(p) && !p.IsInterface);
                    HashSet<INotificationProvider> providers = new HashSet<INotificationProvider>();

                    foreach(var type in types)
                    {
                        INotificationProvider? instance = (INotificationProvider) sp.GetService(type);
                        providers.Add(instance);
                    }

                    return (IEnumerable<INotificationProvider>) providers;
                });
                services.AddSingleton<INotificationService, NotificationService>();

                services.AddHttpClient<BinanceApiClient>().AddPolicyHandler(p =>
                    HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .WaitAndRetryAsync(20, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                            retryAttempt))));

                services.RemoveAll<IHttpMessageHandlerBuilderFilter>();

                CultureInfo ci = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;
            }).Build().Run();
    }
}