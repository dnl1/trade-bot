using System.Collections.Generic;

namespace TradeBot.Database
{
    internal class InMemoryDatabase<T> : IDatabase<T> where T : class
    {
        private Dictionary<string, T> Db { get; set; }
        private readonly object _locker;

        public InMemoryDatabase()
        {
            Db = new Dictionary<string, T>();
            _locker = new object();
        }

        public IEnumerable<T> GetAll()
        {
            foreach (var key in Db.Keys)
            {
                yield return Db[key];
            }
        }

        public T? GetByKey(string id)
        {
            if (Db.ContainsKey(id))
            {
                return Db[id];
            }

            return default;
        }

        public void Save(string id, T obj)
        {
            lock (_locker)
            {
                if (Db.ContainsKey(id))
                {
                    Db[id] = obj;
                }
                else
                {
                    Db.Add(id, obj);
                }
            }
        }
    }
}
