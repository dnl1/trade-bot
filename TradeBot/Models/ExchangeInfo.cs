using System.Collections.Generic;
using Newtonsoft.Json;

namespace TradeBot.Models
{
    public partial class ExchangeInfo
    {
        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        [JsonProperty("serverTime")]
        public long ServerTime { get; set; }

        [JsonProperty("rateLimits")]
        public List<RateLimit> RateLimits { get; set; }

        [JsonProperty("exchangeFilters")]
        public List<object> ExchangeFilters { get; set; }

        [JsonProperty("symbols")]
        public List<Symbol> Symbols { get; set; }
    }

    public partial class RateLimit
    {
        [JsonProperty("rateLimitType")]
        public string RateLimitType { get; set; }

        [JsonProperty("interval")]
        public string Interval { get; set; }

        [JsonProperty("intervalNum")]
        public long IntervalNum { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }
    }

    public partial class Symbol
    {
        [JsonProperty("symbol")]
        public string SymbolName { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("baseAsset")]
        public string BaseAsset { get; set; }

        [JsonProperty("baseAssetPrecision")]
        public long BaseAssetPrecision { get; set; }

        [JsonProperty("quoteAsset")]
        public string QuoteAsset { get; set; }

        [JsonProperty("quotePrecision")]
        public decimal QuotePrecision { get; set; }

        [JsonProperty("quoteAssetPrecision")]
        public decimal QuoteAssetPrecision { get; set; }

        [JsonProperty("baseCommissionPrecision")]
        public decimal BaseCommissionPrecision { get; set; }

        [JsonProperty("quoteCommissionPrecision")]
        public decimal QuoteCommissionPrecision { get; set; }

        [JsonProperty("orderTypes")]
        public List<string> OrderTypes { get; set; }

        [JsonProperty("icebergAllowed")]
        public bool IcebergAllowed { get; set; }

        [JsonProperty("ocoAllowed")]
        public bool OcoAllowed { get; set; }

        [JsonProperty("quoteOrderQtyMarketAllowed")]
        public bool QuoteOrderQtyMarketAllowed { get; set; }

        [JsonProperty("isSpotTradingAllowed")]
        public bool IsSpotTradingAllowed { get; set; }

        [JsonProperty("isMarginTradingAllowed")]
        public bool IsMarginTradingAllowed { get; set; }

        [JsonProperty("filters")]
        public List<Filter> Filters { get; set; }

        [JsonProperty("permissions")]
        public List<string> Permissions { get; set; }
    }

    public partial class Filter
    {
        [JsonProperty("filterType")]
        public string FilterType { get; set; }

        [JsonProperty("minPrice", NullValueHandling = NullValueHandling.Ignore)]
        public string MinPrice { get; set; }

        [JsonProperty("maxPrice", NullValueHandling = NullValueHandling.Ignore)]
        public string MaxPrice { get; set; }

        [JsonProperty("tickSize", NullValueHandling = NullValueHandling.Ignore)]
        public string TickSize { get; set; }

        [JsonProperty("multiplierUp", NullValueHandling = NullValueHandling.Ignore)]
        public string MultiplierUp { get; set; }

        [JsonProperty("multiplierDown", NullValueHandling = NullValueHandling.Ignore)]
        public string MultiplierDown { get; set; }

        [JsonProperty("avgPriceMins", NullValueHandling = NullValueHandling.Ignore)]
        public long? AvgPriceMins { get; set; }

        [JsonProperty("minQty", NullValueHandling = NullValueHandling.Ignore)]
        public string MinQty { get; set; }

        [JsonProperty("maxQty", NullValueHandling = NullValueHandling.Ignore)]
        public string MaxQty { get; set; }

        [JsonProperty("stepSize", NullValueHandling = NullValueHandling.Ignore)]
        public string StepSize { get; set; }

        [JsonProperty("minNotional", NullValueHandling = NullValueHandling.Ignore)]
        public string MinNotional { get; set; }

        [JsonProperty("applyToMarket", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ApplyToMarket { get; set; }

        [JsonProperty("limit", NullValueHandling = NullValueHandling.Ignore)]
        public long? Limit { get; set; }

        [JsonProperty("maxNumOrders", NullValueHandling = NullValueHandling.Ignore)]
        public long? MaxNumOrders { get; set; }

        [JsonProperty("maxNumAlgoOrders", NullValueHandling = NullValueHandling.Ignore)]
        public long? MaxNumAlgoOrders { get; set; }

        [JsonProperty("maxPosition", NullValueHandling = NullValueHandling.Ignore)]
        public string MaxPosition { get; set; }
    }
}
