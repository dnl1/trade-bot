
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using TradeBot.Responses;
using System.Runtime;

namespace TradeBot
{
    internal class OrderGuard : IDisposable
    {
        private readonly ConcurrentDictionary<long, OrderUpdateResult> _pendingOrders;
        private readonly Dictionary<long, ManualResetEventSlim> _mutexes;
        private long _orderId;

        public OrderGuard(ConcurrentDictionary<long, OrderUpdateResult> pendingOrders, Dictionary<long, ManualResetEventSlim> mutexes)
        {
            _pendingOrders = pendingOrders;
            _mutexes = mutexes;
        }

        internal void SetOrder(long orderId)
        {
            _orderId = orderId;
            _mutexes.Add(orderId, new ManualResetEventSlim(false));
        }

        internal ManualResetEventSlim GetMutex()
        {
            return _mutexes[_orderId];
        }

        internal void Wait()
        {
            if(_mutexes.ContainsKey(_orderId))
                _mutexes[_orderId].Wait();
        }

        internal bool ContainsOrder()
        {
            return _pendingOrders.ContainsKey(_orderId);
        }

        internal OrderUpdateResult Get()
        {
            return _pendingOrders[_orderId];
        }

        public void Dispose()
        {
            _pendingOrders.TryRemove(_orderId, out _);
            _mutexes.Remove(_orderId, out _);
        }
    }
}