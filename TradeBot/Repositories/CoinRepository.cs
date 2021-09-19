using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Database;

namespace TradeBot.Repositories
{
    internal class CoinRepository : ICoinRepository
    {
        private readonly IDatabase<Coin> _database;
        private const string CURRENT_COIN = "CURRENT_COIN";

        public CoinRepository(IDatabase<Coin> database)
        {
            _database = database;
        }

        public void SaveCurrent(Coin coin)
        {
            _database.Save(CURRENT_COIN, coin);
        }

        public Coin GetCurrent() => _database.GetByKey(CURRENT_COIN);
    }
}
