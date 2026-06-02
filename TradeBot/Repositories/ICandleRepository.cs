using System.Collections.Generic;
using TradeBot.Models;

namespace TradeBot.Repositories
{
    public interface ICandleRepository
    {
        void AddCandle(Candle candle);

        /// <summary>
        /// Overwrites the live (still-open) candle for the symbol.
        /// Called on every aggTrade update so the strategy always sees the latest bar.
        /// </summary>
        void UpdateLive(Candle candle);

        /// <summary>
        /// Returns the last <paramref name="count"/> closed candles followed by
        /// the current live (open) candle if one exists, so callers always have
        /// the most recent partial bar.
        /// </summary>
        IReadOnlyList<Candle> GetCandles(string symbol, int count);
    }
}
