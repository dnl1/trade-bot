using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using TradeBot.Database;
using Xunit;

namespace TradeBot.Tests.Database
{
    public class CacherTests
    {
        [Fact]
        public void Execute_should_cache_value_within_ttl()
        {
            var cacher = new Cacher();
            var calls = 0;

            var first = cacher.Execute(() => ++calls, TimeSpan.FromMinutes(1));
            var second = cacher.Execute(() => ++calls, TimeSpan.FromMinutes(1));

            first.Should().Be(1);
            second.Should().Be(1);
            calls.Should().Be(1);
        }

        [Fact]
        public void Execute_should_recompute_after_ttl_expires()
        {
            var cacher = new Cacher();
            var calls = 0;

            var first = cacher.Execute(() => ++calls, TimeSpan.FromMilliseconds(10));
            Thread.Sleep(30);
            var second = cacher.Execute(() => ++calls, TimeSpan.FromMilliseconds(10));

            first.Should().Be(1);
            second.Should().Be(2);
            calls.Should().Be(2);
        }

        [Fact]
        public async Task ExecuteAsync_should_use_key_in_cache_identity()
        {
            var cacher = new Cacher();
            var calls = 0;

            var first = await cacher.ExecuteAsync(async () =>
            {
                await Task.Yield();
                return ++calls;
            }, TimeSpan.FromMinutes(1), "btc");

            var second = await cacher.ExecuteAsync(async () =>
            {
                await Task.Yield();
                return ++calls;
            }, TimeSpan.FromMinutes(1), "eth");

            first.Should().Be(1);
            second.Should().Be(2);
        }

        [Fact]
        public void Execute_should_not_cache_null_values()
        {
            var cacher = new Cacher();
            var calls = 0;

            string? Factory()
            {
                calls++;
                return null;
            }

            var first = cacher.Execute(Factory, TimeSpan.FromMinutes(1));
            var second = cacher.Execute(Factory, TimeSpan.FromMinutes(1));

            first.Should().BeNull();
            second.Should().BeNull();
            calls.Should().Be(2);
        }
    }
}
