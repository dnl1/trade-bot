using FluentAssertions;
using TradeBot.Repositories;
using TradeBot.Tests.Builders.Models;
using Xunit;

namespace TradeBot.Tests.Repositories
{
    public class CandleRepositoryTests
    {
        [Fact]
        public void GetCandles_should_return_empty_for_unknown_symbol()
        {
            var repository = new CandleRepository();

            var result = repository.GetCandles("BTCUSDT", 10);

            result.Should().BeEmpty();
        }

        [Fact]
        public void GetCandles_should_return_only_last_requested_count()
        {
            var repository = new CandleRepository();
            for (var i = 0; i < 5; i++)
            {
                repository.AddCandle(new CandleBuilder().WithSymbol("BTCUSDT").WithClose(i).Build());
            }

            var result = repository.GetCandles("BTCUSDT", 2);

            result.Should().HaveCount(2);
            result[0].Close.Should().Be(3);
            result[1].Close.Should().Be(4);
        }

        [Fact]
        public void AddCandle_should_trim_history_to_maximum_size()
        {
            var repository = new CandleRepository();
            for (var i = 0; i < 205; i++)
            {
                repository.AddCandle(new CandleBuilder().WithSymbol("BTCUSDT").WithClose(i).Build());
            }

            var result = repository.GetCandles("BTCUSDT", 300);

            result.Should().HaveCount(200);
            result[0].Close.Should().Be(5);
            result[^1].Close.Should().Be(204);
        }

        [Fact]
        public void AddCandle_should_isolate_symbols()
        {
            var repository = new CandleRepository();
            repository.AddCandle(new CandleBuilder().WithSymbol("BTCUSDT").Build());
            repository.AddCandle(new CandleBuilder().WithSymbol("ETHUSDT").Build());

            repository.GetCandles("BTCUSDT", 10).Should().ContainSingle();
            repository.GetCandles("ETHUSDT", 10).Should().ContainSingle();
        }
    }
}
