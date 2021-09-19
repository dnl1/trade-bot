using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Database;

namespace TradeBot.Repositories
{
    internal class SnapshotRepository : ISnapshotRepository
    {
        private readonly IDatabase _database;

        public SnapshotRepository(IDatabase database)
        {
            _database = database;
        }

        public void Save(Snapshot snapshot)
        {
            _database.Save($"{snapshot.Symbol}", snapshot);
        }

        public Snapshot Get(string symbol)
        {
            return _database.GetById<Snapshot>(symbol);
        }
    }
}
