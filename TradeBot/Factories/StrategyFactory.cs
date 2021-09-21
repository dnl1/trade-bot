using System;
using System.Collections.Generic;
using TradeBot.Strategies;

namespace TradeBot.Factories
{
    internal class StrategyFactory
    {
        private Dictionary<string, Func<AutoTrader>> _strategies;

        public StrategyFactory(IServiceProvider sp)
        {
            _strategies = new Dictionary<string, Func<AutoTrader>>()
            {
                ["default"] = () => (AutoTrader) sp.GetService(typeof(DefaultStrategy))
            };
        }

        public AutoTrader GetStrategy(string strategy)
        {
            if (_strategies.ContainsKey(strategy))
            {
                return _strategies[strategy]();
            }

            return _strategies["default"]();
        }
    }
}
