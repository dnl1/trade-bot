using System.Collections.Generic;
using Newtonsoft.Json;

namespace TradeBot.Models
{
    public class Account
    {
        [JsonProperty("makerCommission")]
        public long MakerCommission { get; set; }

        [JsonProperty("takerCommission")]
        public long TakerCommission { get; set; }

        [JsonProperty("buyerCommission")]
        public long BuyerCommission { get; set; }

        [JsonProperty("sellerCommission")]
        public long SellerCommission { get; set; }

        [JsonProperty("canTrade")]
        public bool CanTrade { get; set; }

        [JsonProperty("canWithdraw")]
        public bool CanWithdraw { get; set; }

        [JsonProperty("canDeposit")]
        public bool CanDeposit { get; set; }

        [JsonProperty("updateTime")]
        public long UpdateTime { get; set; }

        [JsonProperty("accountType")]
        public string AccountType { get; set; } = null!;

        [JsonProperty("balances")]
        public List<Balance> Balances { get; set; } = new();

        [JsonProperty("permissions")]
        public List<string> Permissions { get; set; } = new();
    }

    public class Balance
    {
        [JsonProperty("asset")]
        public string Asset { get; set; } = null!;

        [JsonProperty("free")]
        public decimal Free { get; set; }

        [JsonProperty("locked")]
        public decimal Locked { get; set; }
    }
}
