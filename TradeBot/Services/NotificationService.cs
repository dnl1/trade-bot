using System.Threading.Tasks;
using System.Collections.Generic;

namespace TradeBot.Services
{
    internal class NotificationService : INotificationService
    {
        private readonly IEnumerable<INotificationProvider> _providers;

        public NotificationService(IEnumerable<INotificationProvider> providers)
        {
            _providers = providers;
        }

        public async Task Notify(string msg)
        {
            foreach (var item in _providers)
            {
                await item.SendMessage(msg);
            }
        }
    }
}
