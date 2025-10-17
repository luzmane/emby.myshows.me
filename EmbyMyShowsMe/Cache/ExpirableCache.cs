using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EmbyMyShowsMe.Cache
{
    internal class ExpirableCache<TKey, TValue>: IDisposable
    {
        private readonly Dictionary<TKey, CacheItem<TValue>> _cache = new Dictionary<TKey, CacheItem<TValue>>();
        private readonly Timer _timer;

        public ExpirableCache()
        {
            _timer = new Timer(OnTimerCallback, null, TimeSpan.FromMinutes(30), Timeout.InfiniteTimeSpan);
        }

        public void Store(TKey key, TValue value, TimeSpan expiresAfter)
        {
            _cache[key] = new CacheItem<TValue>(value, expiresAfter);
        }

        public TValue Get(TKey key)
        {
            if (!_cache.TryGetValue(key, out CacheItem<TValue> cached))
            {
                return default;
            }

            if (DateTimeOffset.Now - cached.Created >= cached.ExpiresAfter)
            {
                _cache.Remove(key);
                return default;
            }

            return cached.Value;
        }

        private void OnTimerCallback(object state)
        {
            foreach (var item in _cache.Where(kv => DateTimeOffset.Now - kv.Value.Created >= kv.Value.ExpiresAfter).ToList())
            {
                _cache.Remove(item.Key);
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
