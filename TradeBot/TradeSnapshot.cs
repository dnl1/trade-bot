using Newtonsoft.Json;

namespace TradeBot
{
    internal class TradeSnapshot
    {
        [JsonProperty("e")]
        public string EventType { get; set; }

        [JsonProperty("E")]
        public long EventTime { get; set; }

        [JsonProperty("s")]
        public string Symbol { get; set; }

        [JsonProperty("a")]
        public long SellerOrderId { get; set; }

        [JsonProperty("b")]
        public long BuyerOrderId { get; set; }

        [JsonProperty("p")]
        public decimal Price { get; set; }

        [JsonProperty("q")]
        public decimal Quantity { get; set; }

        [JsonProperty("T")]
        public long TradeTime { get; set; }

        [JsonProperty("m")]
        public bool IsMarketMaker { get; set; }

        [JsonProperty("M")]
        public bool Ignore { get; set; }

    }
}
