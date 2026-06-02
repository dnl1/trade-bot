using TradeBot.Models;

namespace TradeBot.Tests.Builders.Models
{
    public class SnapshotBuilder
    {
        private string _symbol = "BTCUSDT";
        private long _tradeTime;
        private decimal _price = 100m;
        private string _quantity = "1.5";

        public SnapshotBuilder WithSymbol(string symbol)
        {
            _symbol = symbol;
            return this;
        }

        public SnapshotBuilder WithTradeTime(long tradeTime)
        {
            _tradeTime = tradeTime;
            return this;
        }

        public SnapshotBuilder WithPrice(decimal price)
        {
            _price = price;
            return this;
        }

        public SnapshotBuilder WithQuantity(string quantity)
        {
            _quantity = quantity;
            return this;
        }

        public Snapshot Build() =>
            new()
            {
                Symbol = _symbol,
                TradeTime = _tradeTime,
                Price = _price,
                Quantity = _quantity,
                EventType = "aggTrade",
                EventTime = _tradeTime,
                AggTradeId = 1,
                FirstTradeId = 1,
                LastTradeId = 1,
                IsMarketMaker = false,
                Ignore = false
            };
    }
}
