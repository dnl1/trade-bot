using FluentAssertions;
using TradeBot.Factories;
using TradeBot.Strategies;
using TradeBot.Tests.Helpers;
using Xunit;

namespace TradeBot.Tests.Factories
{
    public class StrategyFactoryTests
    {
        [Fact]
        public void GetStrategy_should_return_null_for_unknown_strategy()
        {
            var serviceProvider = new FakeServiceProvider();
            var factory = new StrategyFactory(serviceProvider);

            var result = factory.GetStrategy("unknown");

            result.Should().BeNull();
        }

        [Fact]
        public void GetStrategy_should_return_registered_strategy_instance()
        {
            var strategy = (DefaultStrategy)FormatterServicesProxy.GetUninitializedObject(typeof(DefaultStrategy));
            var serviceProvider = new FakeServiceProvider()
                .Register(strategy);
            var factory = new StrategyFactory(serviceProvider);

            var result = factory.GetStrategy("default");

            result.Should().BeSameAs(strategy);
        }

        [Fact]
        public void GetStrategy_should_support_bollinger_bands_strategy()
        {
            var strategy = (BollingerBandsStrategy)FormatterServicesProxy.GetUninitializedObject(typeof(BollingerBandsStrategy));
            var serviceProvider = new FakeServiceProvider()
                .Register(strategy);
            var factory = new StrategyFactory(serviceProvider);

            var result = factory.GetStrategy("bollingerBands");

            result.Should().BeSameAs(strategy);
        }
    }
}
