using TradeBot.Database;
using TradeBot.Entities;
using System.Collections.Generic;

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

        public void Save(IEnumerable<string> coins)
        {
            foreach (var coin in coins)
            {
                _database.Save(coin, new Coin(coin));
            }
        }

        public IEnumerable<Coin> GetAll()
        {
            return _database.GetAll();
        }

        public Coin GetCurrent() => _database.GetByKey(CURRENT_COIN);
    }
}
