using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    internal class OrderStatus
    {
        public const string FILLED = "FILLED";
        public const string NEW = "NEW";
        public const string PARTIALLY_FILLED = "PARTIALLY_FILLED";
        public const string CANCELED = "CANCELED";
    }
}
