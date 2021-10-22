namespace TradeBot.Entities
{
    public class Coin
    {
        public string Symbol { get; }
        
        public Coin(string symbol)
        {
            Symbol = symbol;
        }
    }
}
