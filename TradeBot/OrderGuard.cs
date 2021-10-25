
using System.Collections.Generic;
using System.Threading;

namespace TradeBot
{
    internal class OrderGuard
    {
        private Dictionary<string, int> _pendingOrders;
        private readonly Dictionary<long, ManualResetEventSlim> _mutexes;

        public OrderGuard(Dictionary<string, int> pendingOrders, Dictionary<long, ManualResetEventSlim> mutexes)
        {
            _pendingOrders = pendingOrders;
            _mutexes = mutexes;
        }

        internal void SetOrder(long orderId)
        {
            _mutexes.Add(orderId, new ManualResetEventSlim(false));
        }

        internal ManualResetEventSlim GetMutex(long orderId)
        {
            return _mutexes[orderId];
        }

        internal void Wait(long orderId)
        {
            if(_mutexes.ContainsKey(orderId))
                _mutexes[orderId].Wait();
        }
    }
}