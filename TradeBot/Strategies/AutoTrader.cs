using System.Linq;
using TradeBot.Entities;
using TradeBot.Repositories;
using TradeBot.Settings;

namespace TradeBot.Strategies
{
    public abstract class AutoTrader
    {
        public readonly IPairRepository _pairRepository;
        public readonly ISnapshotRepository _snapshotRepository;
        private readonly AppSettings _appSettings;
        private readonly BinanceApiManager _manager;
        private readonly ILogger _logger;
        private readonly ICoinRepository _coinRepository;
        private readonly Coin BRIDGE;

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
            BRIDGE = new Coin(appSettings.Bridge);
        }

        public virtual void Initialize()
        {
            InitializeTradeThresholds();
        }

        public virtual void Scout()
        {
            throw new NotImplementedException();
        }

        public async Task TransactionThroughBridge(Pair pair)
        {
            bool canSell = false;
            var balance = await _manager.GetCurrencyBalance(pair.FromCoin.Symbol);
            decimal fromCoinPrice = (await _manager.GetTickerPrice(pair.FromCoin.Symbol + _appSettings.Bridge)).Value;
            decimal minNotional = await _manager.GetMinNotional(pair.FromCoin.Symbol, _appSettings.Bridge);

            if (balance > 0 && balance * fromCoinPrice > minNotional)
                canSell = true;
            else
                _logger.Info("Skipping sell");

            if (canSell)
            {
                try
                {
                    await _manager.SellAlt(pair.FromCoin, BRIDGE);
                }
                catch (Exception e)
                {
                    _logger.Warn($"Couldn't sell, going back to scouting mode... Exception: {e}");
                    return;
                }
            }

            var order = await _manager.BuyAlt(pair.ToCoin, BRIDGE);

            if (null != order)
            {
                _coinRepository.SaveCurrent(pair.ToCoin);
                await UpdateTradeThreshold(pair.ToCoin, order.Price);
                return;
            }

            _logger.Info("Couldn't buy, going back to scouting mode...");
        }

        private async Task UpdateTradeThreshold(Coin coin, decimal price)
        {
            foreach (var pair in _pairRepository.GetPairsTo(coin))
            {
                var coinPrice = await _manager.GetTickerPrice(pair.FromCoin.Symbol + _appSettings.Bridge);

                if (coinPrice.GetValueOrDefault(0) == 0)
                {
                    _logger.Info($"Skipping update for coin {pair.ToCoin + _appSettings.Bridge} not found");
                    continue;
                }

                pair.Ratio = coinPrice.Value / price;

                _pairRepository.Save(pair);
            }
        }

        private void InitializeTradeThresholds()
        {
            var snapshots = _snapshotRepository.GetAll().ToList();
            var pairsFromDb = _pairRepository.GetAll().ToList();

            if (pairsFromDb.Count == snapshots.Count)
                return;

            var pairs = snapshots.SelectMany(x => snapshots, (x, y) => new Pair
            {
                FromCoin = new Coin(x.Symbol.Replace(_appSettings.Bridge, string.Empty)),
                ToCoin = new Coin(y.Symbol.Replace(_appSettings.Bridge, string.Empty))
            }).Where(p => p.FromCoin.Symbol != p.ToCoin.Symbol);

            foreach (var item in pairs)
            {
                var fromPrice = _snapshotRepository.Get(item.FromCoin.Symbol + _appSettings.Bridge).Price;
                var toPrice = _snapshotRepository.Get(item.ToCoin.Symbol + _appSettings.Bridge).Price;
                item.Ratio = fromPrice / toPrice;

                _pairRepository.Save(item);
            }
        }
    }
}
