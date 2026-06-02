using FluentAssertions;
using TradeBot.Entities;
using Xunit;

namespace TradeBot.Tests.Entities
{
    public class PairTests
    {
        [Fact]
        public void Should_construct_with_from_and_to_coins()
        {
            var pair = new Pair
            {
                FromCoin = new Coin("BTC"),
                ToCoin = new Coin("ETH"),
                Ratio = 10.5m
            };

            pair.FromCoin.Symbol.Should().Be("BTC");
            pair.ToCoin.Symbol.Should().Be("ETH");
            pair.Ratio.Should().Be(10.5m);
        }

        [Fact]
        public void Ratio_should_default_to_zero()
        {
            var pair = new Pair();

            pair.Ratio.Should().Be(0m);
        }

        [Fact]
        public void Should_allow_updating_ratio()
        {
            var pair = new Pair
            {
                FromCoin = new Coin("BTC"),
                ToCoin = new Coin("ETH"),
                Ratio = 10m
            };

            pair.Ratio = 15.5m;

            pair.Ratio.Should().Be(15.5m);
        }
    }
}
