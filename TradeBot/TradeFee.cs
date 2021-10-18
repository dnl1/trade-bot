using Newtonsoft.Json;

namespace TradeBot
{
    internal class TradeFee
    {
        [JsonProperty("takerCommission")]
        public decimal TakerCommission { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }
}
