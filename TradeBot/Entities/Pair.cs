using System.Diagnostics;

namespace TradeBot.Entities
{
    [DebuggerDisplay("[{_fromCoinSymbol}]vs[{_toCoinSymbol}]")]
    public class Pair
    {
        private string _fromCoinSymbol => FromCoin?.Symbol;
        private string _toCoinSymbol => ToCoin?.Symbol;
        public Coin FromCoin { get; set; }
        public Coin ToCoin { get; set; }
        public decimal Ratio { get; set; }
    }
}
