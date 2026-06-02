using TradeBot.Models;

namespace TradeBot.Tests.Builders.Models
{
    public class CandleBuilder
    {
        private string _symbol = "BTCUSDT";
        private long _openTime;
        private long _closeTime = 59_999;
        private decimal _open = 100m;
        private decimal _high = 100m;
        private decimal _low = 100m;
        private decimal _close = 100m;
        private decimal _volume = 1m;

        public CandleBuilder WithSymbol(string symbol)
        {
            _symbol = symbol;
            return this;
        }

        public CandleBuilder WithClose(decimal close)
        {
            _close = close;
            return this;
        }

        public CandleBuilder WithHigh(decimal high)
        {
            _high = high;
            return this;
        }

        public CandleBuilder WithLow(decimal low)
        {
            _low = low;
            return this;
        }

        public CandleBuilder WithOpen(decimal open)
        {
            _open = open;
            return this;
        }

        public CandleBuilder WithVolume(decimal volume)
        {
            _volume = volume;
            return this;
        }

        public CandleBuilder WithWindow(long openTime, long closeTime)
        {
            _openTime = openTime;
            _closeTime = closeTime;
            return this;
        }

        public Candle Build() =>
            new()
            {
                Symbol = _symbol,
                OpenTime = _openTime,
                CloseTime = _closeTime,
                Open = _open,
                High = _high,
                Low = _low,
                Close = _close,
                Volume = _volume
            };
    }
}
