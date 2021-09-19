using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Database
{
    internal interface IDatabase
    {
        public void Save<T>(string id, T obj);
        public T? GetById<T>(string id);
    }
}
