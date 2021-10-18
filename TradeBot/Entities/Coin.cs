namespace TradeBot.Entities
{
    public class Coin
    {
        public string Symbol { get; }
        public string FullSymbol { get; }
        
        public Coin(string symbol)
        {
            Symbol = symbol;
        }
    }
}
