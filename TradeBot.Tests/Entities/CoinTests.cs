using FluentAssertions;
using TradeBot.Entities;
using Xunit;

namespace TradeBot.Tests.Entities
{
    public class CoinTests
    {
        [Fact]
        public void Should_construct_coin_from_symbol()
        {
            // Arrange
            var symbol = "BTC";
            
            // Act
            var coin = new Coin(symbol);
            
            // Assert
            coin.Symbol.Should().Be(symbol);
        } 
    }
}