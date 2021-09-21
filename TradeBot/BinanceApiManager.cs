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
            decimal fromCoinPrice = GetTickerPrice(originSymbol + targetSymbol);

            await GetAccount();

        }

        internal async Task GetAccount()
        {
            await _apiClient.GetAccount();
        }

        private decimal GetTickerPrice(string symbol)
        {
            return _snapshotRepository.Get(symbol).Price;
            
        }

    }
}
