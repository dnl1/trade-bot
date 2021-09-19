using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Repositories;

namespace TradeBot.Strategies
{
    internal class DefaultStrategy : AutoTrader
    {
        public DefaultStrategy(IPairRepository pairRepository, ISnapshotRepository snapshotRepository) : base(pairRepository, snapshotRepository)
        {
            InitializeCurrentCoin();
        }

        private void InitializeCurrentCoin()
        {
            throw new NotImplementedException();
        }
    }
}
