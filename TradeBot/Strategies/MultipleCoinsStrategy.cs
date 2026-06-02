using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeBot.Entities;
using TradeBot.Repositories;
using TradeBot.Settings;

namespace TradeBot.Strategies
{
    internal class MultipleCoinsStrategy : AutoTrader
    {
        private readonly ICoinRepository _coinRepository;
        private readonly ILogger _logger;
        private readonly BinanceApiManager _manager;

        public MultipleCoinsStrategy(IPairRepository pairRepository,
            ISnapshotRepository snapshotRepository,
            ICoinRepository coinRepository,
            ILogger logger,
            BinanceApiManager manager,
            AppSettings appSettings) : base(pairRepository, snapshotRepository, appSettings, manager, logger, coinRepository)
        {
            _coinRepository = coinRepository;
            _logger = logger;
            _manager = manager;
        }

        public override async Task Scout()
        {
            _logger.Debug("Running scouting process");

            var haveCoin = false;

            var currentCoin = _coinRepository.GetCurrent();
            string? currentCoinSymbol = null;

            if(null != currentCoin)
            {
                currentCoinSymbol = currentCoin.Symbol;
            }

            foreach (var coinSymbol in _appSettings.Coins)
            {
                var coinBalance = await _manager.GetCurrencyBalance(coinSymbol);
                var coinPrice = await _manager.GetTickerPrice(coinSymbol + _appSettings.Bridge);

                if (!coinPrice.HasValue)
                {
                    _logger.Info($"Skipping scout... current coin {coinSymbol} not found");
                    continue;
                }

                var minNotional = await _manager.GetMinNotional(coinSymbol, _appSettings.Bridge);

                if (coinSymbol != currentCoinSymbol && coinPrice.Value * coinBalance < minNotional)
                    continue;

                haveCoin = true;

                _logger.Debug($"I am scouting best trades. Current Coin {coinSymbol + _appSettings.Bridge}");

                await JumpToBestCoin(new Coin(coinSymbol), coinPrice.Value);

                // Stop after the first trade executed — avoid firing multiple trades in a
                // single scout cycle which could exhaust bridge balance or double-commit.
                break;
            }

            if (!haveCoin)
                await BridgeScout();
        }
    }
}
