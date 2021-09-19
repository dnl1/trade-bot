using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Database
{
    internal class InMemoryDatabase : IDatabase
    {
        public Dictionary<string, object> Db { get; set; }
        private object _locker;

        public InMemoryDatabase()
        {
            Db = new Dictionary<string, object>();
            _locker = new object();
        }

        public T? GetById<T>(string id)
        {
            if (Db.ContainsKey(id))
            {
                return (T)Db[id];
            }

            return default;
        }

        public void Save<T>(string id, T obj)
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
