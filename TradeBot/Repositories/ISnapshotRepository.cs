using System.Collections.Generic;
using TradeBot.Models;

namespace TradeBot.Repositories
{
    internal interface ISnapshotRepository
    {
        Snapshot Get(string symbol);
        void Save(Snapshot snapshot);
        IEnumerable<Snapshot> GetAll();
    }
}