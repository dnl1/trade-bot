using System.Collections.Concurrent;
using System.Collections.Generic;
using TradeBot.Responses;

namespace TradeBot.Repositories
{
    internal static class BinanceCache
    {
        public static ConcurrentDictionary<long, OrderUpdateResult> Orders { get; set; } = new ConcurrentDictionary<long, OrderUpdateResult>();
    }
}
