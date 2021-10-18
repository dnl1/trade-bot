using System.Collections.Generic;
using TradeBot.Responses;

namespace TradeBot.Repositories
{
    internal static class BinanceCache
    {
        public static Dictionary<long, OrderUpdateResult> Orders { get; set; } = new Dictionary<long, OrderUpdateResult>();
    }
}
