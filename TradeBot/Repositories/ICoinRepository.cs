using TradeBot.Entities;
using System.Collections.Generic;

namespace TradeBot.Repositories
{
    public interface ICoinRepository
    {
        Coin GetCurrent();
        void Save(IEnumerable<string> coins);
        void SaveCurrent(Coin coin);
    }
}