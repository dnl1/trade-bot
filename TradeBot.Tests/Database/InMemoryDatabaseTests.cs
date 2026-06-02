using FluentAssertions;
using System.Linq;
using TradeBot.Database;
using Xunit;

namespace TradeBot.Tests.Database
{
    public class InMemoryDatabaseTests
    {
        [Fact]
        public void Save_should_store_and_get_by_key()
        {
            var db = new InMemoryDatabase<TestEntity>();

            db.Save("key1", new TestEntity { Value = "hello" });

            var result = db.GetByKey("key1");
            result.Should().NotBeNull();
            result!.Value.Should().Be("hello");
        }

        [Fact]
        public void Save_should_overwrite_existing_key()
        {
            var db = new InMemoryDatabase<TestEntity>();

            db.Save("key1", new TestEntity { Value = "first" });
            db.Save("key1", new TestEntity { Value = "second" });

            var result = db.GetByKey("key1");
            result!.Value.Should().Be("second");
        }

        [Fact]
        public void GetByKey_should_return_null_for_missing_key()
        {
            var db = new InMemoryDatabase<TestEntity>();

            var result = db.GetByKey("nonexistent");

            result.Should().BeNull();
        }

        [Fact]
        public void GetAll_should_return_all_entries()
        {
            var db = new InMemoryDatabase<TestEntity>();

            db.Save("a", new TestEntity { Value = "A" });
            db.Save("b", new TestEntity { Value = "B" });
            db.Save("c", new TestEntity { Value = "C" });

            var all = db.GetAll().ToList();

            all.Should().HaveCount(3);
            all.Select(e => e.Value).Should().BeEquivalentTo("A", "B", "C");
        }

        [Fact]
        public void GetAll_should_return_empty_when_no_entries()
        {
            var db = new InMemoryDatabase<TestEntity>();

            var all = db.GetAll();

            all.Should().BeEmpty();
        }

        private class TestEntity
        {
            public string Value { get; set; } = string.Empty;
        }
    }
}
