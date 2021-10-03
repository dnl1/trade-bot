using System.Threading.Tasks;
using TradeBot.Repositories;
using TradeBot.Settings;

namespace TradeBot
{
    internal class BinanceApiManager
    {
        private readonly string _baseUrl;
        private readonly AppSettings _settings;
        private readonly ILogger _logger;
        private readonly ISnapshotRepository _snapshotRepository;
        private readonly BinanceApiClient _apiClient;
        private readonly BinanceCache _cache;

        public BinanceApiManager(AppSettings settings, ILogger logger, ISnapshotRepository snapshotRepository, BinanceApiClient apiClient)
        {
            _settings = settings;
            _logger = logger;
            _snapshotRepository = snapshotRepository;
            _apiClient = apiClient;
        }

        internal async Task BuyAlt(Coin origin, Coin target)
        {
            var trade = new Trade(origin, target, Side.BUY);

            string originSymbol = origin.Symbol;
            string targetSymbol = target.Symbol;

            var originBalance = await GetCurrencyBalance(originSymbol);
            var targetBalance = await GetCurrencyBalance(targetSymbol);

            decimal fromCoinPrice = GetTickerPrice(originSymbol + targetSymbol);

            double orderQty = await BuyQuantity(originSymbol, targetSymbol, targetBalance, fromCoinPrice);

            _logger.Information($"BUY QTY {orderQty} of <{originSymbol}>");


        }

        private async Task<double> BuyQuantity(string originSymbol, string targetSymbol, decimal? targetBalance, decimal? fromCoinPrice)
        {
            targetBalance = targetBalance ?? (await GetCurrencyBalance(targetSymbol));
            fromCoinPrice = fromCoinPrice ?? (GetTickerPrice(originSymbol + targetSymbol));

            decimal originTick = await GetAltTick(originSymbol, targetSymbol);

            double originDbl = 2;

            //        return math.floor(target_balance * 10 ** origin_tick / from_coin_price) / float(10 ** origin_tick)


            return Math.Floor((double)targetBalance * Math.Pow(10, (double)originTick) / (double)fromCoinPrice) / Math.Pow(10, (double)originTick);
        }

        private async Task<decimal> GetAltTick(string originSymbol, string targetSymbol)
        {
            var symbol = await _apiClient.GetSymbolInfo(originSymbol + targetSymbol);
            var filter = symbol.Filters.Find(a => a.FilterType == "LOT_SIZE");
            var stepSize = filter.StepSize;

            if (stepSize.IndexOf('1') == 0)
                return 1 - stepSize.IndexOf('.');

            return stepSize.IndexOf('1') - 1;
        }

        private async Task<decimal> GetCurrencyBalance(string targetSymbol)
        {
            var account = await GetAccount();

            return account.Balances.Find(b => b.Asset.Equals(targetSymbol, StringComparison.Ordinal)).Free;
        }

        internal async Task<Account> GetAccount()
        {
            return await _apiClient.GetAccount();
        }

        private decimal GetTickerPrice(string symbol)
        {
            return _snapshotRepository.Get(symbol). Price;
            
        }

    }
}
