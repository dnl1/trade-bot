using System.Collections.Generic;

namespace TradeBot.Repositories
{
    internal interface IPairRepository
    {
        Pair? Get(string fromSymbol, string toSymbol);
        IEnumerable<Pair> GetAll();
        void Save(Pair pair);
    }
}