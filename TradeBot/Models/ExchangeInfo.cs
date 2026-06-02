using System.Collections.Generic;
using Newtonsoft.Json;

namespace TradeBot.Models
{
    public partial class ExchangeInfo
    {
        [JsonProperty("timezone")]
        public string Timezone { get; set; } = null!;

        [JsonProperty("serverTime")]
        public long ServerTime { get; set; }

        [JsonProperty("rateLimits")]
        public List<RateLimit> RateLimits { get; set; } = new();

        [JsonProperty("exchangeFilters")]
        public List<object> ExchangeFilters { get; set; } = new();

        [JsonProperty("symbols")]
        public List<Symbol> Symbols { get; set; } = new();
    }

    public partial class RateLimit
    {
        [JsonProperty("rateLimitType")]
        public string RateLimitType { get; set; } = null!;

        [JsonProperty("interval")]
        public string Interval { get; set; } = null!;

        [JsonProperty("intervalNum")]
        public long IntervalNum { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }
    }

    public partial class Symbol
    {
        [JsonProperty("symbol")]
        public string SymbolName { get; set; } = null!;

        [JsonProperty("status")]
        public string Status { get; set; } = null!;

        [JsonProperty("baseAsset")]
        public string BaseAsset { get; set; } = null!;

        [JsonProperty("baseAssetPrecision")]
        public long BaseAssetPrecision { get; set; }

        [JsonProperty("quoteAsset")]
        public string QuoteAsset { get; set; } = null!;

        [JsonProperty("quotePrecision")]
        public decimal QuotePrecision { get; set; }

        [JsonProperty("quoteAssetPrecision")]
        public decimal QuoteAssetPrecision { get; set; }

        [JsonProperty("baseCommissionPrecision")]
        public decimal BaseCommissionPrecision { get; set; }

        [JsonProperty("quoteCommissionPrecision")]
        public decimal QuoteCommissionPrecision { get; set; }

        [JsonProperty("orderTypes")]
        public List<string> OrderTypes { get; set; } = new();

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
        public List<Filter> Filters { get; set; } = new();

        [JsonProperty("permissions")]
        public List<string> Permissions { get; set; } = new();
    }

    public partial class Filter
    {
        [JsonProperty("filterType")]
        public string FilterType { get; set; } = null!;

        [JsonProperty("minPrice", NullValueHandling = NullValueHandling.Ignore)]
        public string MinPrice { get; set; } = null!;

        [JsonProperty("maxPrice", NullValueHandling = NullValueHandling.Ignore)]
        public string MaxPrice { get; set; } = null!;

        [JsonProperty("tickSize", NullValueHandling = NullValueHandling.Ignore)]
        public string TickSize { get; set; } = null!;

        [JsonProperty("multiplierUp", NullValueHandling = NullValueHandling.Ignore)]
        public string MultiplierUp { get; set; } = null!;

        [JsonProperty("multiplierDown", NullValueHandling = NullValueHandling.Ignore)]
        public string MultiplierDown { get; set; } = null!;

        [JsonProperty("avgPriceMins", NullValueHandling = NullValueHandling.Ignore)]
        public long? AvgPriceMins { get; set; }

        [JsonProperty("minQty", NullValueHandling = NullValueHandling.Ignore)]
        public string MinQty { get; set; } = null!;

        [JsonProperty("maxQty", NullValueHandling = NullValueHandling.Ignore)]
        public string MaxQty { get; set; } = null!;

        [JsonProperty("stepSize", NullValueHandling = NullValueHandling.Ignore)]
        public string StepSize { get; set; } = null!;

        [JsonProperty("minNotional", NullValueHandling = NullValueHandling.Ignore)]
        public decimal MinNotional { get; set; }

        [JsonProperty("applyToMarket", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ApplyToMarket { get; set; }

        [JsonProperty("limit", NullValueHandling = NullValueHandling.Ignore)]
        public long? Limit { get; set; }

        [JsonProperty("maxNumOrders", NullValueHandling = NullValueHandling.Ignore)]
        public long? MaxNumOrders { get; set; }

        [JsonProperty("maxNumAlgoOrders", NullValueHandling = NullValueHandling.Ignore)]
        public long? MaxNumAlgoOrders { get; set; }

        [JsonProperty("maxPosition", NullValueHandling = NullValueHandling.Ignore)]
        public string MaxPosition { get; set; } = null!;
    }
}
