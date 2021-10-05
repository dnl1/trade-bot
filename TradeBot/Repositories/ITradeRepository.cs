using System.Collections.Generic;
using TradeBot.Entities;

namespace TradeBot.Repositories
{
    public interface ITradeRepository
    {
        IEnumerable<Trade> GetAll();
        void Save(Trade trade);
    }
}