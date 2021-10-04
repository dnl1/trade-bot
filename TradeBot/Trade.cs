using System;

namespace TradeBot
{
    public class Trade
    {
        public Trade()
        {
            Date = DateTime.Now;
        }

        public Trade(Coin altCoin, Coin cryptoCoin, Side side)
        {
            AltCoin = altCoin;
            CryptoCoin = cryptoCoin;
            Side = side;
            State = TradeState.Starting;
            Date = DateTime.Now;
        }


        public decimal AltStartingBalance { get; set; }
        public double AltTradeAmount { get; set; }
        public decimal CryptoStartingBalance { get; set; }
        public TradeState State { get; set; }
        public Coin AltCoin { get; }
        public Coin CryptoCoin { get; }
        public Side Side { get; }
        public DateTime Date { get; set; }
    }
}
