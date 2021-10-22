using System;
using System.Threading;
using System.Threading.Tasks;
using FluentScheduler;
using Microsoft.Extensions.Hosting;
using TradeBot.Factories;
using TradeBot.Repositories;
using TradeBot.Settings;

namespace TradeBot.HostedServices
{
    internal class TradingService : IHostedService
    {
        private readonly AppSettings _settings;
        private readonly MarketDataListenerService _marketDataListenerService;
        private readonly StrategyFactory _strategyFactory;
        private readonly ICoinRepository _coinRepository;
        private readonly ILogger _logger;

        public TradingService(AppSettings settings,
            MarketDataListenerService marketDataListenerService,
            StrategyFactory strategyFactory,
            ICoinRepository coinRepository,
            ILogger logger)
        {
            _settings = settings;
            _marketDataListenerService = marketDataListenerService;
            _strategyFactory = strategyFactory;
            _coinRepository = coinRepository;
            _logger = logger;
        }

        /// <summary>
        /// Principal service
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Starting");

            _coinRepository.Save(_settings.Coins);

            await _marketDataListenerService.StartAsync(cancellationToken);

            var strategy = _strategyFactory.GetStrategy(_settings.Strategy);

            if(null == strategy)
            {
                _logger.Error("Invalid strategy name");
                return;
            }

            _logger.Info($"Chosen strategy: {_settings.Strategy}");

            _logger.Info("Waiting for snapshots to load");
            _marketDataListenerService.GetCountdownEvent().Wait();
            _logger.Info("Snapshots loaded with success");

            await strategy.Initialize();

            await strategy.Scout();
            JobManager.AddJob(async () => await strategy.Scout(), s => s.ToRunEvery(_settings.ScoutSleepTime).Minutes());
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
