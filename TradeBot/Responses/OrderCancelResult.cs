using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Responses
{
    internal class OrderCancelResult
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; } = null!;

        [JsonProperty("orderId")]
        public long OrderId { get; set; }

        [JsonProperty("origClientOrderId")]
        public string OrigClientOrderId { get; set; } = null!;

        [JsonProperty("clientOrderId")]
        public string ClientOrderId { get; set; } = null!;
    }
}
