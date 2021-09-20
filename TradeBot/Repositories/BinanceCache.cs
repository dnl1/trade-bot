using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Database;

namespace TradeBot.Repositories
{
    internal class BinanceCache
    {
        public Dictionary<string, decimal> TickerValues { get; set; } = new Dictionary<string, decimal>();
    }
}
