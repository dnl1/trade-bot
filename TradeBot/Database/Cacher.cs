using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Database
{
    public class Cacher : ICacher
    {
        private Dictionary<Type, Tuple<DateTime, object>> _cacher;

        public Cacher()
        {
            _cacher = new Dictionary<Type, Tuple<DateTime, object>>();
        }

        public T ExecuteAsync<T>(Func<T> impl, TimeSpan ttl)
        {
            var type = typeof(T);

            if (_cacher.ContainsKey(type))
            {
                var tpl = _cacher[type];

                if (tpl.Item1 <= DateTime.Now)
                {
                    return (T)tpl.Item2;
                }
            }
            else
            {
                _cacher.Add(type, null);
            }

            T response = impl();

            if (null != response)
                _cacher[type] = Tuple.Create<DateTime, object>(DateTime.Now.Add(ttl), response);

            return response;
        }

        public T Execute<T>(Func<T> impl, TimeSpan ttl)
        {
            var type = typeof(T);

            if (_cacher.ContainsKey(type))
            {
                var tpl = _cacher[type];

                if (tpl.Item1 <= DateTime.Now)
                {
                    return (T) tpl.Item2;
                }
            }
            else
            {
                _cacher.Add(type, null);
            }

            T response = impl();

            if(null != response)
                _cacher[type] = Tuple.Create<DateTime, object>(DateTime.Now.Add(ttl), response);

            return response;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> impl, TimeSpan ttl)
        {
            var type = typeof(T);

            if (_cacher.ContainsKey(type))
            {
                var tpl = _cacher[type];

                if (tpl.Item1 <= DateTime.Now)
                {
                    return (T)tpl.Item2;
                }
            }
            else
            {
                _cacher.Add(type, null);
            }

            T response = await impl();

            if (null != response)
                _cacher[type] = Tuple.Create<DateTime, object>(DateTime.Now.Add(ttl), response);

            return response;
        }
    }
}
