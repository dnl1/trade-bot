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
