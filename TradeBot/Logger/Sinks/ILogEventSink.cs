using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Logger
{
    public interface ILogEventSink
    {
        void Emit(LogEvent logEvent);
    }
}
