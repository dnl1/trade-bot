using TradeBot.Database;

namespace TradeBot.Repositories
{
    internal class TradeRepository : ITradeRepository
    {
        private readonly IDatabase<Trade> _database;
        private int COUNTER = 0;

        public TradeRepository(IDatabase<Trade> database)
        {
            _database = database;
        }

        public void Save(Trade trade)
        {
            _database.Save((++COUNTER).ToString(), trade);
        }

        public IEnumerable<Trade> GetAll() => _database.GetAll();
    }
}
