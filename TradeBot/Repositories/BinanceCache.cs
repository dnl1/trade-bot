using System.Collections.Generic;

namespace TradeBot.Repositories
{
    internal class BinanceCache
    {
        public Dictionary<string, decimal> TickerValues { get; set; } = new Dictionary<string, decimal>();
    }
}
