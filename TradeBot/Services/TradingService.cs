using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Factories;
using TradeBot.Repositories;
using TradeBot.Services;
using TradeBot.Settings;

namespace TradeBot
{
    internal class TradingService : IHostedService
    {
        private readonly AppSettings _settings;
        private readonly MarketDataListenerService _marketDataListenerService;
        private readonly StrategyFactory _strategyFactory;
        private readonly ILogger _logger;

        public TradingService(AppSettings settings, 
            MarketDataListenerService marketDataListenerService,
            StrategyFactory strategyFactory,
            ILogger logger)
        {
            _settings = settings;
            _marketDataListenerService = marketDataListenerService;
            _strategyFactory = strategyFactory;
            _logger = logger;
        }

        /// <summary>
        /// Principal service
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Starting");

            await _marketDataListenerService.StartAsync(cancellationToken);

            var strategy = _strategyFactory.GetStrategy(_settings.Strategy);

            _marketDataListenerService.GetCountdownEvent().Wait();

            strategy.InitializeTradeThresholds();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
