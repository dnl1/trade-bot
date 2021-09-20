using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    internal class Trade
    {
        public Trade(Coin altCoin, Coin cryptoCoin, Side side)
        {
            AltCoin = altCoin;
            CryptoCoin = cryptoCoin;
            Side = side;
            State = TradeState.Starting;
            Date = DateTime.Now;
        }

        public Coin AltCoin { get; }
        public Coin CryptoCoin { get; }
        public Side Side { get; }
        public DateTime Date { get; set; }
        public TradeState State { get; set; }
    }
}
