using System;
using System.Collections.Generic;
using TradeBot.Repositories;

namespace TradeBot.Models
{
    /// <summary>
    /// Aggregates aggTrade snapshots into fixed-time OHLCV candles.
    /// Called from the WebSocket subscriber thread — not thread-safe by design.
    /// </summary>
    public class CandleAggregator
    {
        private readonly long _bucketMs;
        private readonly ICandleRepository _repository;
        private readonly Dictionary<string, Candle> _open = new();

        public CandleAggregator(int timeframeMinutes, ICandleRepository repository)
        {
            _bucketMs = (long)TimeSpan.FromMinutes(timeframeMinutes).TotalMilliseconds;
            _repository = repository;
        }

        public void Process(Snapshot snapshot)
        {
            var openTime = (snapshot.TradeTime / _bucketMs) * _bucketMs;
            var closeTime = openTime + _bucketMs - 1;
            decimal.TryParse(snapshot.Quantity, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var qty);

            if (_open.TryGetValue(snapshot.Symbol, out var current))
            {
                if (snapshot.TradeTime > current.CloseTime)
                {
                    // Flush completed candle, start a new one
                    _repository.AddCandle(current);
                    current = NewCandle(snapshot, openTime, closeTime, qty);
                    _open[snapshot.Symbol] = current;
                }
                else
                {
                    if (snapshot.Price > current.High) current.High = snapshot.Price;
                    if (snapshot.Price < current.Low)  current.Low  = snapshot.Price;
                    current.Close  = snapshot.Price;
                    current.Volume += qty;
                }
            }
            else
            {
                current = NewCandle(snapshot, openTime, closeTime, qty);
                _open[snapshot.Symbol] = current;
            }

            // Always publish the latest open candle so GetCandles() never misses the current bar
            _repository.UpdateLive(current);
        }

        private static Candle NewCandle(Snapshot s, long openTime, long closeTime, decimal qty) =>
            new()
            {
                Symbol    = s.Symbol,
                OpenTime  = openTime,
                CloseTime = closeTime,
                Open      = s.Price,
                High      = s.Price,
                Low       = s.Price,
                Close     = s.Price,
                Volume    = qty,
            };
    }
}
