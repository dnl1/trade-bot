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

        public BinanceApiManager(AppSettings settings) => _settings = settings;
    }
}
