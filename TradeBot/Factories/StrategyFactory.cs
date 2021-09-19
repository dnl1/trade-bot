using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Repositories;
using TradeBot.Strategies;

namespace TradeBot.Factories
{
    internal class StrategyFactory
    {
        private Dictionary<string, Func<AutoTrader>> _strategies;

        public StrategyFactory(IPairRepository pairRepository, ISnapshotRepository snapshotRepository)
        {
            _strategies = new Dictionary<string, Func<AutoTrader>>()
            {
                ["default"] = () => new DefaultStrategy(pairRepository, snapshotRepository)
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
