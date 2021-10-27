using System.Collections.Generic;
using TradeBot.Models;

namespace TradeBot.Repositories
{
    public interface ISnapshotRepository
    {
        Snapshot Get(string symbol);
        void Save(Snapshot snapshot);
        IEnumerable<Snapshot> GetAll();
    }
}