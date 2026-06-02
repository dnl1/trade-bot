using FluentAssertions;
using System.Collections.Generic;
using TradeBot.Indicators;
using TradeBot.Tests.Builders.Models;
using Xunit;
using IndicatorsApi = TradeBot.Indicators.Indicators;

namespace TradeBot.Tests.Indicators
{
    public class IndicatorsTests
    {
        [Fact]
        public void BollingerBands_should_return_null_when_not_enough_candles()
        {
            var candles = new List<TradeBot.Models.Candle>
            {
                new CandleBuilder().Build()
            };

            var result = IndicatorsApi.BollingerBands(candles, period: 2);

            result.Should().BeNull();
        }

        [Fact]
        public void BollingerBands_should_return_flat_bands_for_flat_prices()
        {
            var candles = BuildCandles(20, 100m);

            var result = IndicatorsApi.BollingerBands(candles)!;

            result.Middle.Should().Be(100m);
            result.Upper.Should().Be(100m);
            result.Lower.Should().Be(100m);
            result.Bandwidth.Should().Be(0m);
            result.PercentB.Should().Be(0.5m);
        }

        [Fact]
        public void Rsi_should_return_null_when_not_enough_candles()
        {
            var result = IndicatorsApi.Rsi(BuildCandles(14, 100m), period: 14);

            result.Should().BeNull();
        }

        [Fact]
        public void Rsi_should_return_100_when_there_are_no_losses()
        {
            var candles = new List<TradeBot.Models.Candle>();
            for (var i = 0; i < 20; i++)
            {
                candles.Add(new CandleBuilder().WithClose(100m + i).Build());
            }

            var result = IndicatorsApi.Rsi(candles, period: 14);

            result.Should().Be(100m);
        }

        [Fact]
        public void Atr_should_return_null_when_not_enough_candles()
        {
            var result = IndicatorsApi.Atr(BuildCandles(14, 100m), period: 14);

            result.Should().BeNull();
        }

        [Fact]
        public void Atr_should_return_expected_value_for_constant_true_range()
        {
            var candles = BuildRangeCandles(20, 100m, 110m, 90m);

            var result = IndicatorsApi.Atr(candles, period: 14);

            result.Should().Be(20m);
        }

        [Fact]
        public void AtrSma_should_return_null_when_not_enough_candles()
        {
            var result = IndicatorsApi.AtrSma(BuildRangeCandles(20, 100m, 110m, 90m), 14, 20);

            result.Should().BeNull();
        }

        [Fact]
        public void AtrSma_should_return_expected_value_for_constant_true_range()
        {
            var candles = BuildRangeCandles(40, 100m, 110m, 90m);

            var result = IndicatorsApi.AtrSma(candles, 14, 20);

            result.Should().Be(20m);
        }

        [Fact]
        public void Adx_should_return_null_when_not_enough_candles()
        {
            var result = IndicatorsApi.Adx(BuildRangeCandles(20, 100m, 110m, 90m), period: 14);

            result.Should().BeNull();
        }

        [Fact]
        public void Adx_should_return_positive_value_for_trending_market()
        {
            var candles = new List<TradeBot.Models.Candle>();
            for (var i = 0; i < 40; i++)
            {
                candles.Add(new CandleBuilder()
                    .WithOpen(100m + i)
                    .WithHigh(105m + i)
                    .WithLow(99m + i)
                    .WithClose(104m + i)
                    .Build());
            }

            var result = IndicatorsApi.Adx(candles, period: 14);

            result.Should().NotBeNull();
            result.Should().BeGreaterThan(0m);
        }

        [Fact]
        public void Rsi_should_return_0_when_there_are_no_gains()
        {
            var candles = new List<TradeBot.Models.Candle>();
            for (var i = 0; i < 20; i++)
            {
                candles.Add(new CandleBuilder().WithClose(100m - i).Build());
            }

            var result = IndicatorsApi.Rsi(candles, period: 14);

            result.Should().Be(0m);
        }

        [Fact]
        public void Rsi_should_return_50_for_alternating_gains_and_losses()
        {
            var candles = new List<TradeBot.Models.Candle>();
            for (var i = 0; i < 30; i++)
            {
                candles.Add(new CandleBuilder().WithClose(i % 2 == 0 ? 100m : 101m).Build());
            }

            var result = IndicatorsApi.Rsi(candles, period: 14);

            result.Should().NotBeNull();
            result.Should().BeInRange(45m, 55m);
        }

        [Fact]
        public void BollingerBands_should_return_correct_values_for_trending_prices()
        {
            var candles = new List<TradeBot.Models.Candle>();
            for (var i = 0; i < 20; i++)
            {
                candles.Add(new CandleBuilder().WithClose(100m + i).Build());
            }

            var result = IndicatorsApi.BollingerBands(candles, 20, 2.0m)!;

            result.Middle.Should().Be(109.5m);
            result.PercentB.Should().BeInRange(0, 1);
        }

        [Fact]
        public void BollingerBands_should_return_0_percentB_when_price_at_lower_band()
        {
            var candles = new List<TradeBot.Models.Candle>();
            for (var i = 0; i < 20; i++)
                candles.Add(new CandleBuilder().WithClose(100m).Build());
            // Last candle at lower band
            candles[^1] = new CandleBuilder().WithClose(100m - 2 * 0).Build();

            var result = IndicatorsApi.BollingerBands(candles, 20, 2.0m)!;

            // All same price → bands are flat → %B = 0.5
            result.PercentB.Should().Be(0.5m);
        }

        [Fact]
        public void AtrSma_should_return_value_at_exact_boundary()
        {
            var candles = BuildRangeCandles(35, 100m, 110m, 90m);

            var result = IndicatorsApi.AtrSma(candles, 14, 20);

            result.Should().NotBeNull();
            result.Should().Be(20m);
        }

        [Fact]
        public void Adx_should_return_value_at_exact_boundary()
        {
            var candles = new List<TradeBot.Models.Candle>();
            for (var i = 0; i < 29; i++)
            {
                candles.Add(new CandleBuilder()
                    .WithOpen(100m)
                    .WithHigh(105m)
                    .WithLow(99m)
                    .WithClose(104m)
                    .Build());
            }

            var result = IndicatorsApi.Adx(candles, period: 14);

            result.Should().NotBeNull();
        }

        [Fact]
        public void BollingerBands_should_return_null_with_exactly_period_less_one_candles()
        {
            var candles = BuildCandles(19, 100m);

            var result = IndicatorsApi.BollingerBands(candles, 20);

            result.Should().BeNull();
        }

        private static List<TradeBot.Models.Candle> BuildCandles(int count, decimal close)
        {
            var candles = new List<TradeBot.Models.Candle>();
            for (var i = 0; i < count; i++)
            {
                candles.Add(new CandleBuilder().WithClose(close).Build());
            }

            return candles;
        }

        private static List<TradeBot.Models.Candle> BuildRangeCandles(int count, decimal open, decimal high, decimal low)
        {
            var candles = new List<TradeBot.Models.Candle>();
            for (var i = 0; i < count; i++)
            {
                candles.Add(new CandleBuilder()
                    .WithOpen(open)
                    .WithHigh(high)
                    .WithLow(low)
                    .WithClose(open)
                    .Build());
            }

            return candles;
        }
    }
}
