using TradeBot.Entities;

namespace TradeBot.Tests.Builders.Entities
{
    public class CoinBuilder
    {
        private string _symbol = "BTC";

        public Coin Build()
        {
            var coin = new Coin(_symbol);

            return coin;
        }
    }
}