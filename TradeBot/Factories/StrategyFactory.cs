using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using TradeBot.Strategies;

namespace TradeBot.Factories
{
    internal class StrategyFactory
    {
        private readonly Dictionary<string, Func<AutoTrader>> _strategies;

        public StrategyFactory(IServiceProvider sp)
        {
            _strategies = new Dictionary<string, Func<AutoTrader>>()
            {
                ["default"] = () => sp.GetRequiredService<DefaultStrategy>(),
                ["multipleCoins"] = () => sp.GetRequiredService<MultipleCoinsStrategy>(),
                ["bollingerBands"] = () => sp.GetRequiredService<BollingerBandsStrategy>()
            };
        }

        public AutoTrader? GetStrategy(string strategy)
        {
            return _strategies.TryGetValue(strategy, out var factory)
                ? factory()
                : null;
        }
    }
}
