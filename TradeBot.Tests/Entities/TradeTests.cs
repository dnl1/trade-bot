using System;
using FluentAssertions;
using TradeBot.Entities;
using TradeBot.Enums;
using TradeBot.Tests.Builders.Entities;
using Xunit;

namespace TradeBot.Tests.Entities
{
    public class TradeTests
    {
        [Fact]
        public void Should_construct_trade_from_balances_and_trade_amount()
        {
            var altStartingBalance = 10;
            var cryptoStartingBalance = 20;
            var altTradeAmount = 10;
            
            // Act
            var trade = new Trade(altStartingBalance, cryptoStartingBalance, altTradeAmount);
            
            // Assert
            trade.AltStartingBalance.Should().Be(altStartingBalance);
            trade.CryptoStartingBalance.Should().Be(cryptoStartingBalance);
            trade.AltTradeAmount.Should().Be(altTradeAmount);
            trade.State.Should().Be(TradeState.Ordered);
            trade.Date.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));

            trade.AltCoin.Should().BeNull();
            trade.CryptoCoin.Should().BeNull();
            trade.Side.Should().BeNull();
        }
        
        [Fact]
        public void Should_construct_trade_from_coins_and_side()
        {
            var altCoin = new CoinBuilder()
                .Build();
            
            var cryptoCoin = new CoinBuilder()
                .Build();
            
            var side = Side.BUY;
            
            // Act
            var trade = new Trade(altCoin, cryptoCoin, side);
            
            // Assert
            trade.AltCoin.Should().Be(altCoin);
            trade.CryptoCoin.Should().Be(cryptoCoin);
            trade.Side.Should().Be(side);
            trade.State.Should().Be(TradeState.Starting);
            trade.Date.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            
            trade.AltStartingBalance.Should().Be(0);
            trade.CryptoStartingBalance.Should().Be(0);
            trade.AltTradeAmount.Should().Be(0);
        }
    }
}