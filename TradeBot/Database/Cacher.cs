using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TradeBot.Database
{
    public class Cacher : ICacher
    {
        private readonly ConcurrentDictionary<string, (DateTime Expiry, object Value)?> _cacher = new();

        // Per-key semaphores prevent the thundering-herd / double-invocation race:
        // two concurrent callers for the same key both miss → both fire impl() → doubled API calls.
        // GetOrAdd is atomic for inserting the semaphore; WaitAsync/Release serialize the actual fetch.
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

        private SemaphoreSlim GetLock(string key) =>
            _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        public T Execute<T>(Func<T> impl, TimeSpan ttl, string key = "")
        {
            var cacheKey = $"{typeof(T).FullName}:{key}";

            // Fast path — no lock needed for a cache hit
            if (_cacher.TryGetValue(cacheKey, out var entry) && entry.HasValue && entry.Value.Expiry >= DateTime.UtcNow)
                if (entry.Value.Value is T cached) return cached;

            var sem = GetLock(cacheKey);
            sem.Wait();
            try
            {
                // Double-check after acquiring the lock
                if (_cacher.TryGetValue(cacheKey, out entry) && entry.HasValue && entry.Value.Expiry >= DateTime.UtcNow)
                    if (entry.Value.Value is T hit) return hit;

                T response = impl();
                if (null != response)
                    _cacher[cacheKey] = (DateTime.UtcNow.Add(ttl), response);
                return response;
            }
            finally { sem.Release(); }
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> impl, TimeSpan ttl, string key = "")
        {
            var cacheKey = $"{typeof(T).FullName}:{key}";

            // Fast path — no lock needed for a cache hit
            if (_cacher.TryGetValue(cacheKey, out var entry) && entry.HasValue && entry.Value.Expiry >= DateTime.UtcNow)
                if (entry.Value.Value is T cached) return cached;

            var sem = GetLock(cacheKey);
            await sem.WaitAsync();
            try
            {
                // Double-check after acquiring the lock
                if (_cacher.TryGetValue(cacheKey, out entry) && entry.HasValue && entry.Value.Expiry >= DateTime.UtcNow)
                    if (entry.Value.Value is T hit) return hit;

                var response = await impl();
                if (null != response)
                    _cacher[cacheKey] = (DateTime.UtcNow.Add(ttl), response);
                return response;
            }
            finally { sem.Release(); }
        }
    }
}
