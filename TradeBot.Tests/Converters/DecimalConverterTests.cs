using FluentAssertions;
using Newtonsoft.Json;
using TradeBot.Converters;
using Xunit;

namespace TradeBot.Tests.Converters
{
    public class DecimalConverterTests
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            Converters = new JsonConverter[] { new DecimalConverter() }
        };

        [Theory]
        [InlineData("42", 42.0)]
        [InlineData("3.14", 3.14)]
        [InlineData("0.00001000", 0.00001000)]
        public void Should_deserialize_string_to_decimal(string input, decimal expected)
        {
            var json = $"{{\"value\": \"{input}\"}}";

            var result = JsonConvert.DeserializeObject<TestModel>(json, Settings);

            result!.Value.Should().Be(expected);
        }

        [Fact]
        public void Should_deserialize_number_to_decimal()
        {
            var json = "{\"value\": 42.5}";

            var result = JsonConvert.DeserializeObject<TestModel>(json, Settings);

            result!.Value.Should().Be(42.5m);
        }

        [Fact]
        public void Should_deserialize_integer_to_decimal()
        {
            var json = "{\"value\": 100}";

            var result = JsonConvert.DeserializeObject<TestModel>(json, Settings);

            result!.Value.Should().Be(100m);
        }

        [Fact]
        public void Should_deserialize_null_to_nullable_decimal()
        {
            var json = "{\"nullableValue\": null}";

            var result = JsonConvert.DeserializeObject<NullableTestModel>(json, Settings);

            result!.NullableValue.Should().BeNull();
        }

        [Fact]
        public void Should_throw_on_unexpected_token()
        {
            var json = "{\"value\": true}";

            var act = () => JsonConvert.DeserializeObject<TestModel>(json, Settings);

            act.Should().Throw<JsonSerializationException>();
        }

        private class TestModel
        {
            public decimal Value { get; set; }
        }

        private class NullableTestModel
        {
            public decimal? NullableValue { get; set; }
        }
    }
}
