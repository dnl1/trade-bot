using System.Collections.Generic;
using TradeBot.Models;
using TradeBot.Repositories;

namespace TradeBot.Tests.Helpers
{
    internal class FakeCandleRepository : ICandleRepository
    {
        public List<Candle> SavedCandles { get; } = new();

        public Candle? LiveCandle { get; private set; }

        public void AddCandle(Candle candle) => SavedCandles.Add(candle);

        public void UpdateLive(Candle candle) => LiveCandle = candle;

        public IReadOnlyList<Candle> GetCandles(string symbol, int count)
        {
            return SavedCandles;
        }
    }
}
