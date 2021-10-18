﻿using System;
using System.Globalization;
using System.Threading;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using TradeBot;
using TradeBot.Database;
using TradeBot.Factories;
using TradeBot.HostedServices;
using TradeBot.Repositories;
using TradeBot.Services;
using TradeBot.Settings;
using TradeBot.Strategies;

IConfiguration? Configuration = null;

Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(builder =>
     {
         builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
             .AddEnvironmentVariables();

         Configuration = builder.Build();
     })
    .ConfigureServices(services =>
    {
        GlobalConfiguration.Configuration.UseMemoryStorage();
        services.AddHostedService<TradingService>();

        var appSettings = new AppSettings();
        Configuration.Bind(appSettings);

        services.AddSingleton(appSettings);
        services.AddSingleton<BinanceApiManager>();
        services.AddSingleton<BinanceStreamManager>();
        services.AddSingleton<BinanceApiClient>();
        services.AddSingleton<MarketDataListenerService>();
        services.AddSingleton<StrategyFactory>();
        services.AddSingleton<DefaultStrategy>();
        services.AddSingleton<ILogger>(new ConsoleLogger("tradebot-logger"));
        services.AddSingleton(typeof(IDatabase<>), typeof(InMemoryDatabase<>));
        services.AddSingleton<ISnapshotRepository, SnapshotRepository>();
        services.AddSingleton<IPairRepository, PairRepository>();
        services.AddSingleton<ICoinRepository, CoinRepository>();
        services.AddSingleton<ITradeRepository, TradeRepository>();
        services.AddSingleton<ITradeService, TradeService>();

        services.AddHttpClient<BinanceApiClient>().AddPolicyHandler(p => 
        HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(20, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                                                                    retryAttempt))));

        CultureInfo ci = new CultureInfo("en-US");
        Thread.CurrentThread.CurrentCulture = ci;
        Thread.CurrentThread.CurrentUICulture = ci;
    }).Build().Run();