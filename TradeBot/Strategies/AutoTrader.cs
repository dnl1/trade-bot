using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Repositories;

namespace TradeBot.Strategies
{
    internal abstract class AutoTrader
    {
        private readonly IPairRepository _pairRepository;
        private readonly ISnapshotRepository _snapshotRepository;

        public AutoTrader(IPairRepository pairRepository, ISnapshotRepository snapshotRepository)
        {
            _pairRepository = pairRepository;
            this._snapshotRepository = snapshotRepository;
        }

        public void TransactionThroughBridge()
        {

        }

        public void InitializeTradeThresholds()
        {
            var snapshots = _snapshotRepository.GetAll().ToList();
            var pairsFromDb = _pairRepository.GetAll().ToList();

            if (pairsFromDb.Count == snapshots.Count)
                return;

            var pairs = snapshots.SelectMany(x => snapshots, (x, y) => new Pair
            {
                FromCoin = new Coin
                {
                    Symbol = x.Symbol,
                },
                ToCoin = new Coin
                {
                    Symbol = y.Symbol
                }
            }).Where(p => p.FromCoin.Symbol != p.ToCoin.Symbol);

            foreach (var item in pairs)
            {
                var fromPrice = _snapshotRepository.Get(item.FromCoin.Symbol).Price;
                var toPrice = _snapshotRepository.Get(item.ToCoin.Symbol).Price;
                item.Ratio = fromPrice / toPrice;

                _pairRepository.Save(item);
            }
        }
    }
}
