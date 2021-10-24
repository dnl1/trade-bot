
using System.Threading.Tasks;

namespace TradeBot.Services
{
    internal interface INotificationService
    {
        Task Notify(string msg);
    }
}