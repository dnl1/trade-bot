using FluentAssertions;
using TradeBot.Entities;
using TradeBot.Enums;
using TradeBot.Services;
using TradeBot.Tests.Helpers;
using Xunit;

namespace TradeBot.Tests.Services
{
    public class TradeServiceTests
    {
        [Fact]
        public void SetOrdered_should_create_and_save_ordered_trade()
        {
            var repository = new FakeTradeRepository();
            var service = new TradeService(repository);

            var trade = service.SetOrdered(10m, 20m, 3d);

            repository.SavedTrades.Should().ContainSingle();
            repository.SavedTrades[0].Should().BeSameAs(trade);
            trade.State.Should().Be(TradeState.Ordered);
        }

        [Fact]
        public void StartTradeLog_should_save_starting_trade()
        {
            var repository = new FakeTradeRepository();
            var service = new TradeService(repository);
            var origin = new Coin("BTC");
            var target = new Coin("ETH");

            service.StartTradeLog(origin, target, Side.BUY);

            repository.SavedTrades.Should().ContainSingle();
            repository.SavedTrades[0].State.Should().Be(TradeState.Starting);
            repository.SavedTrades[0].AltCoin.Should().Be(origin);
            repository.SavedTrades[0].CryptoCoin.Should().Be(target);
            repository.SavedTrades[0].Side.Should().Be(Side.BUY);
        }

        [Fact]
        public void SetComplete_should_update_trade_and_save_again()
        {
            var repository = new FakeTradeRepository();
            var service = new TradeService(repository);
            var trade = new Trade(10m, 20m, 3d);

            service.SetComplete(trade, 55m);

            repository.SavedTrades.Should().ContainSingle();
            repository.SavedTrades[0].Should().BeSameAs(trade);
            trade.CryptoTradingAmount.Should().Be(55m);
            trade.State.Should().Be(TradeState.Complete);
        }
    }
}
