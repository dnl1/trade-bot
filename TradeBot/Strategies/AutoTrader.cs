using System;
using System.Linq;
using System.Threading.Tasks;
using TradeBot.Entities;
using TradeBot.Repositories;
using TradeBot.Settings;
using System.Collections.Generic;

namespace TradeBot.Strategies
{
    public abstract class AutoTrader
    {
        private readonly IPairRepository _pairRepository;
        private readonly ISnapshotRepository _snapshotRepository;
        private readonly ICoinRepository _coinRepository;
        protected readonly AppSettings _appSettings;
        private readonly BinanceApiManager _manager;
        private readonly ILogger _logger;
        protected readonly Coin _bridge;

        public AutoTrader(IPairRepository pairRepository,
            ISnapshotRepository snapshotRepository,
            AppSettings appSettings,
            BinanceApiManager manager,            
            ILogger logger,
            ICoinRepository coinRepository)
        {
            _pairRepository = pairRepository;
            _snapshotRepository = snapshotRepository;
            _appSettings = appSettings;
            _manager = manager;
            _logger = logger;
            _coinRepository = coinRepository;
            _bridge = new Coin(appSettings.Bridge);
        }

        public virtual Task Initialize()
        {
            InitializeTradeThresholds();

            return Task.CompletedTask;
        }

        public virtual Task Scout()
        {
            throw new NotImplementedException();
        }

        public virtual async Task<Coin?> BridgeScout()
        {
            var bridgeBalance = await _manager.GetCurrencyBalance(_appSettings.Bridge);

            foreach (var coinSymbol in _appSettings.Coins)
            {
                var coin = new Coin(coinSymbol);
                var currentCoinPrice = await _manager.GetTickerPrice(coinSymbol + _appSettings.Bridge);

                if (!currentCoinPrice.HasValue) continue;

                var ratioDict = await GetRatios(coin, currentCoinPrice.Value);

                if (ratioDict.Select(i => i.Value).Where(i => i > 0).Any())
                {
                    var minNotional = await _manager.GetMinNotional(coinSymbol, _appSettings.Bridge);
                    if (bridgeBalance > minNotional)
                    {
                        _logger.Info($"Will be purchasing {coinSymbol} using bridge coin");
                        await _manager.BuyAlt(coin, _bridge);
                        return coin;
                    }
                }
            }

            return null;
        }

        public async Task TransactionThroughBridge(Pair pair)
        {
            bool canSell = false;
            var balance = await _manager.GetCurrencyBalance(pair.FromCoin.Symbol);
            var fromCoinPriceResult = await _manager.GetTickerPrice(pair.FromCoin.Symbol + _appSettings.Bridge);
            if (!fromCoinPriceResult.HasValue)
            {
                _logger.Warn($"Skipping transaction — price for {pair.FromCoin.Symbol + _appSettings.Bridge} unavailable");
                return;
            }
            decimal fromCoinPrice = fromCoinPriceResult.Value;
            decimal minNotional = await _manager.GetMinNotional(pair.FromCoin.Symbol, _appSettings.Bridge);

            if (balance > 0 && balance * fromCoinPrice > minNotional)
                canSell = true;
            else
                _logger.Info("Skipping sell");

            if (!canSell)
                return;

            try
            {
                _logger.Info($"Selling {pair.FromCoin.Symbol}");
                await _manager.SellAlt(pair.FromCoin, _bridge);
            }
            catch (Exception e)
            {
                _logger.Warn($"Couldn't sell, going back to scouting mode... Exception: {e}");
                return;
            }

            var order = await _manager.BuyAlt(pair.ToCoin, _bridge);

            if (null != order)
            {
                _coinRepository.SaveCurrent(pair.ToCoin);
                await UpdateTradeThreshold(pair.ToCoin, order.Price);
                return;
            }

            // Buy failed — refresh the pair threshold to current market price so the same
            // trade is not immediately re-attempted on the next scout cycle.
            var fallbackPrice = await _manager.GetTickerPrice(pair.ToCoin.Symbol + _appSettings.Bridge);
            if (fallbackPrice.HasValue)
                await UpdateTradeThreshold(pair.ToCoin, fallbackPrice.Value);

            _logger.Info("Couldn't buy, going back to scouting mode...");
        }

        protected async Task JumpToBestCoin(Coin currentCoin, decimal currentCoinPrice)
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

        protected async Task<Dictionary<Pair, decimal>> GetRatios(Coin currentCoin, decimal coinPrice)
        {
            Dictionary<Pair, decimal> ratioDict = new Dictionary<Pair, decimal>();

            foreach (var pair in _pairRepository.GetPairsFrom(currentCoin))
            {
                var optionalCoinPrice = await _manager.GetTickerPrice(pair.ToCoin.Symbol + _appSettings.Bridge);

                if (!optionalCoinPrice.HasValue || optionalCoinPrice.Value <= 0)

                {
                    _logger.Warn($"Skipping scouting... optional coin {pair.ToCoin.Symbol} not found");
                    continue;
                }

                // db log scout

                //the price of scouting coin / tocoin's price
                decimal optCoinRatio = coinPrice / optionalCoinPrice.Value;

                decimal transactionFee = (await _manager.GetFee(pair.FromCoin, _bridge, true)) + (await _manager.GetFee(pair.ToCoin, _bridge, false));

                decimal ratio = (optCoinRatio - transactionFee * _appSettings.ScoutMultiplier * optCoinRatio) - pair.Ratio;

                ratioDict.Add(pair, ratio);
            }

            return ratioDict;
        }

        private async Task UpdateTradeThreshold(Coin coin, decimal price)
        {
            foreach (var pair in _pairRepository.GetPairsTo(coin))
            {
                var coinPrice = await _manager.GetTickerPrice(pair.FromCoin.Symbol + _appSettings.Bridge);

                if (coinPrice.GetValueOrDefault(0) == 0)
                {
                    _logger.Info($"Skipping update for pair {pair.FromCoin.Symbol + _appSettings.Bridge} — price not found");
                    continue;
                }

                pair.Ratio = coinPrice.Value / price;

                _pairRepository.Save(pair);
            }
        }

        private void InitializeTradeThresholds()
        {
            var snapshots = _snapshotRepository.GetAll().ToList();

            // Always reinitialize from current snapshot prices.
            // Persisted thresholds from a previous session (potentially days old) could
            // trigger spurious trades immediately after a redeploy — fresh prices are safer.
            if (snapshots.Count == 0)
                return; // no live price data yet; defer until snapshots arrive

            var pairs = snapshots.SelectMany(x => snapshots, (x, y) => new Pair
            {
                FromCoin = new Coin(ExtractCoinName(x.Symbol, _appSettings.Bridge)),
                ToCoin = new Coin(ExtractCoinName(y.Symbol, _appSettings.Bridge))
            }).Where(p => p.FromCoin.Symbol != p.ToCoin.Symbol);

            foreach (var item in pairs)
            {
                var fromSnapshot = _snapshotRepository.Get(item.FromCoin.Symbol + _appSettings.Bridge);
                var toSnapshot = _snapshotRepository.Get(item.ToCoin.Symbol + _appSettings.Bridge);
                if (fromSnapshot is null || toSnapshot is null) continue;

                _logger.Info($"Initializing [{item.FromCoin.Symbol}] vs [{item.ToCoin.Symbol}]");
                item.Ratio = fromSnapshot.Price / toSnapshot.Price;

                _pairRepository.Save(item);
            }
        }

        /// <summary>
        /// Extracts the base asset name from a Binance symbol by removing the bridge suffix.
        /// For example, "BTCUSDT" with bridge "USDT" returns "BTC".
        /// Falls back to removing the first occurrence of bridge anywhere in the symbol.
        /// </summary>
        private static string ExtractCoinName(string symbol, string bridge)
        {
            if (symbol.EndsWith(bridge, StringComparison.Ordinal))
                return symbol[..^bridge.Length];
            return symbol.Replace(bridge, "", StringComparison.Ordinal);
        }
    }
}
