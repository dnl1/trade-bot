using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly BinanceCache _cache;
        private readonly HttpClient _httpClient;

        public BinanceApiManager(AppSettings settings, ILogger logger, BinanceCache cache, HttpClient httpClient)
        {
            _settings = settings;
            _logger = logger;
            _cache = cache;
            _httpClient = httpClient;
        }

        internal void BuyAlt(Coin origin, Coin target)
        {
            var trade = new Trade(origin, target, Side.BUY);

            string originSymbol = origin.Symbol;
            string targetSymbol = target.Symbol;
            decimal fromCoinPrice = GetTickerPrice(originSymbol + targetSymbol);

        }

        public decimal GetTickerPrice(string symbol)
        {
            bool ok = _cache.TickerValues.TryGetValue(symbol, out decimal price);
            if (!ok)
            {

            }

            return 0;
        }

    }
}
