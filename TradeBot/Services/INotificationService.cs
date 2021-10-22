
namespace TradeBot.Services
{
    internal interface INotificationService
    {
        Task Notify(string msg);
    }
}