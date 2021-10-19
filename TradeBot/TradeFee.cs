using Newtonsoft.Json;

namespace TradeBot
{
    public class TradeFee
    {
        [JsonProperty("takerCommission")]
        public decimal TakerCommission { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }
}
