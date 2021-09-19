// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeBot;
using TradeBot.Database;
using TradeBot.Repositories;
using TradeBot.Settings;

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
        services.AddHostedService<MarketDataListener>();

        var appSettings = new AppSettings();
        Configuration.Bind(appSettings);

        services.AddSingleton(appSettings);
        services.AddSingleton<IDatabase, InMemoryDatabase>();
        services.AddSingleton<ISnapshotRepository, SnapshotRepository>();
    }).Build().Run();

Thread.Sleep(-1);