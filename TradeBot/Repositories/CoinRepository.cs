using TradeBot.Database;
using TradeBot.Entities;

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
