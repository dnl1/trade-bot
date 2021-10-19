using System.Collections.Generic;
using TradeBot.Entities;

namespace TradeBot.Repositories
{
    public interface IPairRepository
    {
        Pair? Get(string fromSymbol, string toSymbol);
        IEnumerable<Pair> GetAll();
        void Save(Pair pair);
        IEnumerable<Pair> GetPairsFrom(Coin currentCoin);
        IEnumerable<Pair> GetPairsTo(Coin currentCoin);
    }
}