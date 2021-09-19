using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using TradeBot.Repositories;
using TradeBot.Settings;

namespace TradeBot
{
    internal class MarketDataListener : IHostedService
    {
        private readonly AppSettings _settings;
        private readonly ISnapshotRepository _repository;

        public MarketDataListener(AppSettings settings, ISnapshotRepository repository)
        {
            _settings = settings;
            _repository = repository;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var manager = new BinanceStreamManager(_settings);

            var tickers = _settings.Coins.Select(coin => $"{coin}{_settings.Bridge}");

            await manager.SubscribeAsync(tickers, (snapshot) =>
            {
                _repository.Save(snapshot);
            }, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
