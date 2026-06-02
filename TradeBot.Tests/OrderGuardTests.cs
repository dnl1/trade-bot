using System.Collections.Concurrent;
using System.Threading.Tasks;
using FluentAssertions;
using TradeBot.Responses;
using Xunit;

namespace TradeBot.Tests
{
    public class OrderGuardTests
    {
        private static OrderGuard CreateGuard(
            out ConcurrentDictionary<long, OrderUpdateResult> pending,
            out ConcurrentDictionary<long, TaskCompletionSource<OrderUpdateResult>> completions)
        {
            pending = new ConcurrentDictionary<long, OrderUpdateResult>();
            completions = new ConcurrentDictionary<long, TaskCompletionSource<OrderUpdateResult>>();
            return new OrderGuard(pending, completions);
        }

        [Fact]
        public void SetOrder_should_register_in_completions()
        {
            var guard = CreateGuard(out var pending, out var completions);

            guard.SetOrder(42);

            completions.Should().ContainKey(42);
        }

        [Fact]
        public void SetOrder_should_complete_immediately_if_order_already_pending()
        {
            var guard = CreateGuard(out var pending, out var completions);
            var existing = new OrderUpdateResult { OrderId = 42, Status = "FILLED" };
            pending[42] = existing;

            guard.SetOrder(42);

            completions[42].Task.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task WaitAsync_should_return_when_tcs_is_set()
        {
            var guard = CreateGuard(out var pending, out var completions);
            guard.SetOrder(1);
            var expected = new OrderUpdateResult { OrderId = 1, Status = "FILLED" };

            completions[1].TrySetResult(expected);

            var result = await guard.WaitAsync();
            result.Should().BeSameAs(expected);
        }

        [Fact]
        public void TryGet_should_return_null_when_order_not_pending()
        {
            var guard = CreateGuard(out var pending, out var completions);
            guard.SetOrder(1);

            var result = guard.TryGet();

            result.Should().BeNull();
        }

        [Fact]
        public void TryGet_should_return_order_when_pending()
        {
            var guard = CreateGuard(out var pending, out var completions);
            guard.SetOrder(1);
            var expected = new OrderUpdateResult { OrderId = 1, Status = "NEW" };
            pending[1] = expected;

            var result = guard.TryGet();

            result.Should().BeSameAs(expected);
        }

        [Fact]
        public void Dispose_should_remove_from_both_dictionaries()
        {
            var guard = CreateGuard(out var pending, out var completions);
            guard.SetOrder(1);

            guard.Dispose();

            pending.Should().NotContainKey(1);
            completions.Should().NotContainKey(1);
        }

        [Fact]
        public void Dispose_should_be_idempotent()
        {
            var guard = CreateGuard(out var pending, out var completions);
            guard.SetOrder(1);

            guard.Dispose();
            guard.Dispose();

            pending.Should().NotContainKey(1);
        }
    }
}
