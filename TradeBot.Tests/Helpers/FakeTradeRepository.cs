using System.Collections.Generic;
using TradeBot.Entities;
using TradeBot.Repositories;

namespace TradeBot.Tests.Helpers
{
    internal class FakeTradeRepository : ITradeRepository
    {
        public List<Trade> SavedTrades { get; } = new();

        public IEnumerable<Trade> GetAll() => SavedTrades;

        public void Save(Trade trade)
        {
            SavedTrades.Add(trade);
        }
    }
}
