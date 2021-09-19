using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    internal interface ILogger
    {
        void Warning(string message);
        void Error(string message);
        void Information(string message);
        void Debug(string message);
    }
}
