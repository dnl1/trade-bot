using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Responses
{
    internal class ListenKeyWrapper
    {
        [JsonProperty("listenKey")]
        public string ListenKey { get; set; }
    }
}
