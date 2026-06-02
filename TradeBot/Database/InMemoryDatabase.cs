using System.Collections.Generic;
using System.Linq;

namespace TradeBot.Database
{
    internal class InMemoryDatabase<T> : IDatabase<T> where T : class
    {
        private readonly Dictionary<string, T> Db = new();
        private readonly object _locker = new();

        public IEnumerable<T> GetAll()
        {
            lock (_locker)
                return Db.Values.ToList();
        }

        public T? GetByKey(string id)
        {
            lock (_locker)
                return Db.TryGetValue(id, out var val) ? val : default;
        }

        public void Save(string id, T obj)
        {
            lock (_locker)
                Db[id] = obj;
        }
    }
}
