using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Settings;

namespace TradeBot
{
    internal class BinanceApiManager
    {
        private readonly string _baseUrl;
        private readonly AppSettings _settings;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public BinanceApiManager(AppSettings settings, ILogger logger, HttpClient httpClient)
        {
            _settings = settings;
            _logger = logger;
            _httpClient = httpClient;
        }

        internal void BuyAlt(string origin, string target)
        {
            
        }
    }
}
