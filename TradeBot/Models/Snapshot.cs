using Newtonsoft.Json;

namespace TradeBot.Models
{
    public class Snapshot
    {
        [JsonProperty("e")]
        public string EventType { get; set; } = null!;

        [JsonProperty("E")]
        public long EventTime { get; set; }

        [JsonProperty("s")]
        public string Symbol { get; set; } = null!;

        [JsonProperty("a")]
        public long AggTradeId { get; set; }

        [JsonProperty("p")]
        public decimal Price { get; set; }

        [JsonProperty("q")]
        public string Quantity { get; set; } = null!;

        [JsonProperty("f")]
        public long FirstTradeId { get; set; }

        [JsonProperty("l")]
        public long LastTradeId { get; set; }

        [JsonProperty("T")]
        public long TradeTime { get; set; }

        [JsonProperty("m")]
        public bool IsMarketMaker { get; set; }

        [JsonProperty("M")]
        public bool Ignore { get; set; }

    }
}
