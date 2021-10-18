
using System.Collections.Generic;
using System.Threading;

namespace TradeBot
{
    internal class OrderGuard
    {
        private Dictionary<string, int> _pendingOrders;
        private readonly Dictionary<long, Mutex> _slimOrder;

        public OrderGuard(Dictionary<string, int> pendingOrders, Dictionary<long, Mutex> slimOrder)
        {
            _pendingOrders = pendingOrders;
            _slimOrder = slimOrder;
        }

        internal void SetOrder(long orderId)
        {
            _slimOrder.Add(orderId, new Mutex());
        }

        internal Mutex GetMutex(long orderId)
        {
            return _slimOrder[orderId];
        }
    }
}