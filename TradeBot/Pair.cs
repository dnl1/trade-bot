using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    public class Pair
    {
        public Coin FromCoin { get; set; }
        public Coin ToCoin { get; set; }
        public decimal Ratio { get; set; }
    }
}
