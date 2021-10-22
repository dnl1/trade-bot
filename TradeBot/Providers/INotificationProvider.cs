using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot.Services
{
    internal interface INotificationProvider
    {
        Task SendMessage(string msg);
            
    }
}
