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

        public override void Initialize()
        {
            base.Initialize();
            InitializeCurrentCoin().Wait();
        }

        public override void Scout()
        {
            var currentCoin = _coinRepository.GetCurrent();

            _logger.Info($"I am scouting the best trades. Current coin: {currentCoin.Symbol + _appSettings.Bridge} ");

            var currentCoinPrice = _manager.GetTickerPrice(currentCoin.Symbol + _appSettings.Bridge).GetAwaiter().GetResult();

            if (!currentCoinPrice.HasValue)
            {
                _logger.Warn($"Skipping scouting... current coin {currentCoin.Symbol + _appSettings.Bridge} not found");
                return;
            }

            JumpToBestCoin(currentCoin, currentCoinPrice.GetValueOrDefault()).Wait();
        }

        private async Task JumpToBestCoin(Coin currentCoin, decimal currentCoinPrice)
        {
            Dictionary<Pair, decimal> ratioDict = await GetRatios(currentCoin, currentCoinPrice);

            // keep only ratios bigger than zero
            var filteredRatioDict = ratioDict.Where(d => d.Value > 0);

            //if we have any viable options, pick the one with the biggest ratio

            if (filteredRatioDict.Any())
            {
                var bestPair = filteredRatioDict.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
                _logger.Info($"Will be jumping from {currentCoin.Symbol} to {bestPair.ToCoin.Symbol}");
                await TransactionThroughBridge(bestPair);
            }
        }

        private async Task<Dictionary<Pair, decimal>> GetRatios(Coin currentCoin, decimal coinPrice)
        {
            Dictionary<Pair, decimal> ratioDict = new Dictionary<Pair, decimal>();

            foreach (var pair in _pairRepository.GetPairsFrom(new Coin(currentCoin.Symbol)))
            {
                var optionalCoinPrice = await _manager.GetTickerPrice(pair.ToCoin.Symbol + _appSettings.Bridge);

                if (!optionalCoinPrice.HasValue)
                {
                    _logger.Warn($"Skipping scouting... optional coin {pair.ToCoin.Symbol} not found");
                    continue;
                }

                // db log scout

                decimal optCoinRatio = coinPrice / optionalCoinPrice.Value;

                decimal transactionFee = (await _manager.GetFee(pair.FromCoin, BRIDGE, true)) + (await _manager.GetFee(pair.FromCoin, BRIDGE, false));

                decimal ratio = (optCoinRatio - transactionFee * _appSettings.ScoutMultiplier * optCoinRatio) - pair.Ratio;

                ratioDict.Add(pair, ratio);
            }

            return ratioDict;
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
