using System;
using TradeBot.Enums;

namespace TradeBot.Entities
{
    public class Trade
    {
        public decimal AltStartingBalance { get; }
        public decimal CryptoStartingBalance { get; }
        public decimal CryptoTradingAmount { get; private set; }
        public double AltTradeAmount { get; }
        public TradeState State { get; private set; }
        public Coin? AltCoin { get; }
        public Coin? CryptoCoin { get; }
        public Side? Side { get; set; }
        public DateTime Date { get; }
        
        public Trade(decimal altStartingBalance, decimal cryptoStartingBalance, double altTradeAmount)
        {
            AltStartingBalance = altStartingBalance;
            CryptoStartingBalance = cryptoStartingBalance;
            AltTradeAmount = altTradeAmount;
            State = TradeState.Ordered;
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

        public void SetComplete(decimal cryptoTradingAmount)
        {
            CryptoTradingAmount = cryptoTradingAmount;
            State = TradeState.Complete;
        }
    }
}
