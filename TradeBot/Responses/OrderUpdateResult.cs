using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Responses
{
    internal class OrderUpdateResult
    {
        [JsonProperty("e")]
        public string EventType { get; set; }

        [JsonProperty("E")]
        public long EventTime { get; set; }

        [JsonProperty("s")]
        public string Symbol { get; set; }

        [JsonProperty("c")]
        public string ClientOrderId { get; set; }

        [JsonProperty("S")]
        public string Side { get; set; }

        [JsonProperty("o")]
        public string OrderType { get; set; }

        [JsonProperty("f")]
        public string TimeInForce { get; set; }

        [JsonProperty("q")]
        public decimal Quantity { get; set; }

        [JsonProperty("p")]
        public decimal Price { get; set; }

        [JsonProperty("P")]
        public decimal StopPrice { get; set; }

        [JsonProperty("F")]
        public decimal IcebergQuantity { get; set; }

        [JsonProperty("g")]
        public long OrderListId { get; set; }

        [JsonProperty("C")]
        public string CanceledOrderId { get; set; }

        [JsonProperty("x")]
        public string ExecutionType { get; set; }

        [JsonProperty("X")]
        public string Status { get; set; }

        [JsonProperty("r")]
        public string RejectReason { get; set; }

        [JsonProperty("i")]
        public long OrderId { get; set; }

        [JsonProperty("l")]
        public decimal LastExecutedQty{ get; set; }

        [JsonProperty("z")]
        public decimal CumulativeFilledQuantity { get; set; }

        [JsonProperty("L")]
        public string LastExecutedPrice { get; set; }

        [JsonProperty("n")]
        public long CommissionAmount { get; set; }

        [JsonProperty("N")]
        public object CommissionAsset { get; set; }

        [JsonProperty("T")]
        public long TransactionTime { get; set; }

        [JsonProperty("t")]
        public long TradeId { get; set; }

        [JsonProperty("I")]
        public long Ignore { get; set; }

        [JsonProperty("w")]
        public bool IsOrderOnTheBook { get; set; }

        [JsonProperty("m")]
        public bool IsMakerSide { get; set; }

        [JsonProperty("M")]
        public bool IgnoreTwo { get; set; }

        [JsonProperty("O")]
        public long OrderCreationTime { get; set; }

        [JsonProperty("Z")]
        public string CumQuoteAssetTransactedQty { get; set; }

        [JsonProperty("Y")]
        public string LastQuoteAssetTransactedQty { get; set; }

        [JsonProperty("Q")]
        public string QuoteOrderQty { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
