using System.Collections.Generic;

namespace TradeBot.Database
{
    internal interface IDatabase<T> where T : class
    {
        public void Save(string key, T obj);
        public T? GetByKey(string key);
        public IEnumerable<T> GetAll();
    }
}
