
using System.Collections.Generic;
using System.Threading;

namespace TradeBot
{
    internal class OrderGuard
    {
        private Dictionary<string, int> pendingOrders;
        private Mutex mutex;

        public OrderGuard(Dictionary<string, int> pendingOrders, Mutex mutex)
        {
            this.pendingOrders = pendingOrders;
            this.mutex = mutex;
        }
    }
}