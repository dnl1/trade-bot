using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TradeBot.Models;

namespace TradeBot.Repositories
{
    public class CandleRepository : ICandleRepository
    {
        private const int MaxCandles = 200;
        private readonly ConcurrentDictionary<string, List<Candle>> _closed = new();
        private readonly ConcurrentDictionary<string, Candle>       _live   = new();

        public void AddCandle(Candle candle)
        {
            var list = _closed.GetOrAdd(candle.Symbol, _ => new List<Candle>(MaxCandles + 1));
            lock (list)
            {
                list.Add(candle);
                if (list.Count > MaxCandles)
                    list.RemoveAt(0);
            }
            // Remove stale live candle for this bucket (it just closed)
            _live.TryRemove(candle.Symbol, out _);
        }

        public void UpdateLive(Candle candle) =>
            _live[candle.Symbol] = candle;

        public IReadOnlyList<Candle> GetCandles(string symbol, int count)
        {
            _closed.TryGetValue(symbol, out var list);
            _live.TryGetValue(symbol, out var live);

            List<Candle> result;

            if (list is not null)
            {
                lock (list)
                {
                    var take = live is not null ? count - 1 : count;
                    var skip = Math.Max(0, list.Count - take);
                    result = list.Skip(skip).ToList();
                }
            }
            else
            {
                result = new List<Candle>();
            }

            if (live is not null)
                result.Add(live);

            return result;
        }
    }
}
