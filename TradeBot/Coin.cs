using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    public class Coin
    {
        public Coin()
        {
        }

        public Coin(string symbol)
        {
            Symbol = symbol;
        }

        public string Symbol { get; set; }
    }
}
