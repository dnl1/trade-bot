using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    internal class Snapshot
    {
        [JsonProperty("e")]
        public string EventType { get; set; }

        [JsonProperty("E")]
        public long EventTime { get; set; }

        [JsonProperty("s")]
        public string Symbol { get; set; }

        [JsonProperty("a")]
        public long AggTradeId { get; set; }

        [JsonProperty("p")]
        public string Price { get; set; }

        [JsonProperty("q")]
        public string Quantity { get; set; }

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
