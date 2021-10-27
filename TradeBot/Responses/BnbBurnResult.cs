using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Responses
{
    public class BnbBurnResult
    {
        [JsonProperty("spotBNBBurn")]
        public bool SpotBNBBurn { get; set; }

        [JsonProperty("interestBNBBurn")]
        public bool InterestBNBBurn { get; set; }
    }
}
