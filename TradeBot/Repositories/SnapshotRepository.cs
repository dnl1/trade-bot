using System.Collections.Generic;
using TradeBot.Database;
using TradeBot.Models;

namespace TradeBot.Repositories
{
    internal class SnapshotRepository : ISnapshotRepository
    {
        private readonly IDatabase<Snapshot> _database;

        public SnapshotRepository(IDatabase<Snapshot> database)
        {
            _database = database;
        }

        public void Save(Snapshot snapshot)
        {
            _database.Save($"{snapshot.Symbol}", snapshot);
        }

        public Snapshot Get(string symbol) => _database.GetByKey(symbol);
        public IEnumerable<Snapshot> GetAll() => _database.GetAll();
    }
}
