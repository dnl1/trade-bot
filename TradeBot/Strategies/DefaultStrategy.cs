using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeBot.Entities;
using TradeBot.Repositories;
using TradeBot.Settings;

namespace TradeBot.Strategies
{
    internal class DefaultStrategy : AutoTrader
    {
        private readonly ICoinRepository _coinRepository;
        private readonly ILogger _logger;
        private readonly BinanceApiManager _manager;
        private readonly AppSettings _appSettings;
        private readonly Coin BRIDGE;

        public DefaultStrategy(IPairRepository pairRepository,
            ISnapshotRepository snapshotRepository,
            ICoinRepository coinRepository,
            ILogger logger,
            BinanceApiManager manager,
            AppSettings appSettings) : base(pairRepository, snapshotRepository, appSettings, manager, logger, coinRepository)
        {
            _coinRepository = coinRepository;
            _logger = logger;
            _manager = manager;
            _appSettings = appSettings;

            BRIDGE = new Coin(appSettings.Bridge);
        }

        public override async Task Initialize()
        {
            await base.Initialize();
            await InitializeCurrentCoin();
        }

        public override async Task Scout()
        {
            var currentCoin = _coinRepository.GetCurrent();

            _logger.Info($"I am scouting the best trades. Current coin: {currentCoin.Symbol + _appSettings.Bridge} ");

            var currentCoinPrice = await _manager.GetTickerPrice(currentCoin.Symbol + _appSettings.Bridge);

            if (!currentCoinPrice.HasValue)
            {
                _logger.Warn($"Skipping scouting... current coin {currentCoin.Symbol + _appSettings.Bridge} not found");
                return;
            }

            await base.JumpToBestCoin(currentCoin, currentCoinPrice.GetValueOrDefault());
        }

        public override async Task<Coin?> BridgeScout()
        {
            var currentCoin = _coinRepository.GetCurrent();
            var coinBalance = await _manager.GetCurrencyBalance(currentCoin.Symbol);
            var minNotional = await _manager.GetMinNotional(currentCoin.Symbol, _appSettings.Bridge);

            if (coinBalance > minNotional) return null;

            var newCoin = await base.BridgeScout();

            if(null != newCoin)
                _coinRepository.SaveCurrent(newCoin);

            return null;
        }

        private async Task InitializeCurrentCoin()
        {
            var coin = _coinRepository.GetCurrent();

            if (coin is null)
            {
                string currentCoinSymbol = _appSettings.CurrentCoin;

                if (string.IsNullOrEmpty(currentCoinSymbol))
                {
                    var random = new Random();
                    int index = random.Next(_appSettings.Coins.Length);
                    currentCoinSymbol = _appSettings.Coins[index];
                }

                _logger.Info($"Setting initial coin to {currentCoinSymbol}");

                var currentCoin = new Coin(currentCoinSymbol);

                _coinRepository.SaveCurrent(currentCoin);

                _logger.Info($"Purchasing {currentCoinSymbol} to begin trading");

                await _manager.BuyAlt(currentCoin, new Coin(_appSettings.Bridge));

                _logger.Info("Ready to start trading");
            }
        }
    }
}
