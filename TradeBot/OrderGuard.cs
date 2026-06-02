using System.Collections.Concurrent;
using System.Threading.Tasks;
using TradeBot.Responses;

namespace TradeBot
{
    internal class OrderGuard : System.IDisposable
    {
        private readonly ConcurrentDictionary<long, OrderUpdateResult> _pendingOrders;
        private readonly ConcurrentDictionary<long, TaskCompletionSource<OrderUpdateResult>> _orderCompletions;
        private long _orderId;

        public OrderGuard(
            ConcurrentDictionary<long, OrderUpdateResult> pendingOrders,
            ConcurrentDictionary<long, TaskCompletionSource<OrderUpdateResult>> orderCompletions)
        {
            _pendingOrders = pendingOrders;
            _orderCompletions = orderCompletions;
        }

        internal void SetOrder(long orderId)
        {
            _orderId = orderId;
            var tcs = new TaskCompletionSource<OrderUpdateResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _orderCompletions[orderId] = tcs;

            // Race guard: order may have arrived via WebSocket before SetOrder was called
            if (_pendingOrders.TryGetValue(orderId, out var existing))
                tcs.TrySetResult(existing);
        }

        internal Task<OrderUpdateResult> WaitAsync() => _orderCompletions[_orderId].Task;

        internal OrderUpdateResult? TryGet() =>
            _pendingOrders.TryGetValue(_orderId, out var result) ? result : null;

        public void Dispose()
        {
            _pendingOrders.TryRemove(_orderId, out _);
            _orderCompletions.TryRemove(_orderId, out _);
        }
    }
}
