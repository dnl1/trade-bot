using System.Collections.Generic;
using System.Linq;
using TradeBot.Database;
using TradeBot.Entities;

namespace TradeBot.Repositories
{
    public class PairRepository : IPairRepository
    {
        private readonly IDatabase<Pair> _database;

        public PairRepository(IDatabase<Pair> database)
        {
            _database = database;
        }

        public void Save(Pair pair)
        {
            string key = GetKey(pair);
            _database.Save(key, pair);
        }

        public Pair? Get(string fromSymbol, string toSymbol)
        {
            string key = GetKey(fromSymbol, toSymbol);
            return _database.GetByKey(key);
        }

        public IEnumerable<Pair> GetAll() => _database.GetAll();

        private string GetKey(Pair pair)
        {
            return GetKey(pair.FromCoin.Symbol, pair.ToCoin.Symbol);
        }
        private string GetKey(string fromSymbol, string toSymbol)
        {
            return $"{fromSymbol}vs{toSymbol}";
        }

        public IEnumerable<Pair> GetPairsFrom(Coin currentCoin)
        {
            return _database.GetAll().Where(a => a.FromCoin.Symbol.Equals(currentCoin.Symbol));
        }

        public IEnumerable<Pair> GetPairsTo(Coin currentCoin)
        {
            return _database.GetAll().Where(a => a.ToCoin.Symbol.Equals(currentCoin.Symbol));
        }
    }
}
