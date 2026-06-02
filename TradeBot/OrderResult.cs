using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    public partial class OrderResult
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; } = null!;

        [JsonProperty("orderId")]
        public long OrderId { get; set; }

        [JsonProperty("orderListId")]
        public long OrderListId { get; set; }

        [JsonProperty("clientOrderId")]
        public string ClientOrderId { get; set; } = null!;

        [JsonProperty("transactTime")]
        public long TransactTime { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("origQty")]
        public string OrigQty { get; set; } = null!;

        [JsonProperty("executedQty")]
        public string ExecutedQty { get; set; } = null!;

        [JsonProperty("cummulativeQuoteQty")]
        public decimal CummulativeQuoteQty { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = null!;

        [JsonProperty("timeInForce")]
        public string TimeInForce { get; set; } = null!;

        [JsonProperty("type")]
        public string Type { get; set; } = null!;

        [JsonProperty("side")]
        public string Side { get; set; } = null!;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

}
