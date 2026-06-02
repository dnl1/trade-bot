using FluentAssertions;
using TradeBot.Models;
using TradeBot.Tests.Builders.Models;
using TradeBot.Tests.Helpers;
using Xunit;

namespace TradeBot.Tests.Models
{
    public class CandleAggregatorTests
    {
        [Fact]
        public void Process_should_not_persist_first_open_candle_immediately()
        {
            var repository = new FakeCandleRepository();
            var aggregator = new CandleAggregator(1, repository);

            aggregator.Process(new SnapshotBuilder().WithTradeTime(10_000).Build());

            repository.SavedCandles.Should().BeEmpty();
        }

        [Fact]
        public void Process_should_update_existing_candle_within_same_bucket()
        {
            var repository = new FakeCandleRepository();
            var aggregator = new CandleAggregator(1, repository);

            aggregator.Process(new SnapshotBuilder().WithTradeTime(10_000).WithPrice(100m).WithQuantity("1.5").Build());
            aggregator.Process(new SnapshotBuilder().WithTradeTime(20_000).WithPrice(120m).WithQuantity("2.5").Build());
            aggregator.Process(new SnapshotBuilder().WithTradeTime(30_000).WithPrice(90m).WithQuantity("1.0").Build());
            aggregator.Process(new SnapshotBuilder().WithTradeTime(61_000).WithPrice(95m).Build());

            repository.SavedCandles.Should().ContainSingle();
            var candle = repository.SavedCandles[0];
            candle.Open.Should().Be(100m);
            candle.High.Should().Be(120m);
            candle.Low.Should().Be(90m);
            candle.Close.Should().Be(90m);
            candle.Volume.Should().Be(5.0m);
        }

        [Fact]
        public void Process_should_persist_previous_candle_when_bucket_rolls_over()
        {
            var repository = new FakeCandleRepository();
            var aggregator = new CandleAggregator(1, repository);

            aggregator.Process(new SnapshotBuilder().WithTradeTime(59_000).WithPrice(100m).Build());
            aggregator.Process(new SnapshotBuilder().WithTradeTime(61_000).WithPrice(110m).Build());

            repository.SavedCandles.Should().ContainSingle();
            repository.SavedCandles[0].OpenTime.Should().Be(0);
            repository.SavedCandles[0].CloseTime.Should().Be(59_999);
        }

        [Fact]
        public void Process_should_treat_invalid_quantity_as_zero()
        {
            var repository = new FakeCandleRepository();
            var aggregator = new CandleAggregator(1, repository);

            aggregator.Process(new SnapshotBuilder().WithTradeTime(10_000).WithPrice(100m).WithQuantity("abc").Build());
            aggregator.Process(new SnapshotBuilder().WithTradeTime(61_000).WithPrice(110m).Build());

            repository.SavedCandles.Should().ContainSingle();
            repository.SavedCandles[0].Volume.Should().Be(0m);
        }
    }
}
