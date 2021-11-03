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
using TradeBot.Repositories;
using TradeBot.Services;
using TradeBot.Settings;
using TradeBot.Strategies;
using System.Reflection;
using TradeBot.Logger;
using TradeBot.Logger.Sinks;

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

                services.AddLoggers(appSettings);
                services.AddSingleton(appSettings);
                services.AddSingleton<BinanceApiManager>();
                services.AddSingleton<BinanceStreamManager>();
                services.AddSingleton<BinanceApiClient>();
                services.AddSingleton<MarketDataListenerService>();
                services.AddSingleton<StrategyFactory>();
                services.AddSingleton<DefaultStrategy>();
                services.AddSingleton<MultipleCoinsStrategy>();
                services.AddSingleton(typeof(IDatabase<>), typeof(InMemoryDatabase<>));
                services.AddSingleton<ICacher, Cacher>();
                services.AddSingleton<ISnapshotRepository, SnapshotRepository>();
                services.AddSingleton<IPairRepository, PairRepository>();
                services.AddSingleton<ICoinRepository, CoinRepository>();
                services.AddSingleton<ITradeRepository, TradeRepository>();
                services.AddSingleton<ITradeService, TradeService>();

                services.AddHttpClient<BinanceApiClient>(client => client.Timeout = TimeSpan.FromMinutes(5)).AddPolicyHandler(p =>
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