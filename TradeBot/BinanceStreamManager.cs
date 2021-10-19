using FluentScheduler;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TradeBot.Repositories;
using TradeBot.Responses;
using TradeBot.Settings;

namespace TradeBot
{
    public class BinanceStreamManager
    {
        private readonly AppSettings _settings;
        private readonly BinanceApiClient _apiClient;
        private readonly string _baseUrl;
        private readonly Dictionary<string, int> _pendingOrders;
        private readonly Dictionary<long, Mutex> _mutexes;

        public BinanceStreamManager(AppSettings settings, BinanceApiClient binanceApiClient)
        {
            _settings = settings;
            _apiClient = binanceApiClient;
            _baseUrl = "wss://stream.binance.com:9443/ws";

            _mutexes = new Dictionary<long, Mutex>();
            _pendingOrders = new Dictionary<string, int>();
        }

        public void Subscribe<T>(IEnumerable<string> tickers, string streamName, Action<T> action, CancellationToken cancellationToken)
        {
            Task.Factory.StartNew<Task>(async () =>
            {
                ClientWebSocket socket = await CreateSocket(cancellationToken);

                object payload = new
                {
                    method = "SUBSCRIBE",
                    @params = tickers.Select(ticker => $"{ticker.ToLower()}@{streamName}"),
                    id = 1
                };

                string payloadStr = JsonConvert.SerializeObject(payload);
                var sndBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(payloadStr));

                await socket.SendAsync(sndBuffer, WebSocketMessageType.Text, true, cancellationToken);

                var rcvBytes = new byte[1000];
                var rcvBuffer = new ArraySegment<byte>(rcvBytes);

                WebSocketReceiveResult ping = await socket.ReceiveAsync(rcvBuffer, cancellationToken);

                while (true)
                {
                    WebSocketReceiveResult rcvResult = await socket.ReceiveAsync(rcvBuffer, cancellationToken);
                    byte[] msgBytes = rcvBuffer.Skip(rcvBuffer.Offset).Take(rcvResult.Count).ToArray();
                    string rcvMsg = Encoding.UTF8.GetString(msgBytes);
                    var obj = JsonConvert.DeserializeObject<T>(rcvMsg);

                    if (null != obj)
                        action(obj);
                }
            }, TaskCreationOptions.LongRunning);
        }

        internal async Task StreamProcessor()
        {
            var listenKey = await _apiClient.GetListenKey();

            JobManager.AddJob(() => _apiClient.GetListenKey().Wait(),s => s.ToRunEvery(30).Minutes());

            ClientWebSocket socket = await UserStreamCreateSocket(listenKey.ListenKey);

            var rcvBytes = new byte[1000];
            var rcvBuffer = new ArraySegment<byte>(rcvBytes);

            while (true)
            {
                WebSocketReceiveResult rcvResult = await socket.ReceiveAsync(rcvBuffer, new CancellationToken());
                byte[] msgBytes = rcvBuffer.Skip(rcvBuffer.Offset).Take(rcvResult.Count).ToArray();
                string rcvMsg = Encoding.UTF8.GetString(msgBytes);
                var obj = JsonConvert.DeserializeObject<OrderUpdateResult>(rcvMsg);

                if (null != obj && obj.OrderId > 0)
                {
                    BinanceCache.Orders[obj.OrderId] = obj;
                    _mutexes[obj.OrderId].ReleaseMutex();
                }
            }
        }

        private async Task<ClientWebSocket> UserStreamCreateSocket(string listenKey)
        {
            var socket = new ClientWebSocket();

            string url = $"{_baseUrl}/{listenKey}";
            await socket.ConnectAsync(new Uri(url), new CancellationToken());
            return socket;
        }

        private async Task<ClientWebSocket> CreateSocket(CancellationToken cancellationToken)
        {
            var socket = new ClientWebSocket();

            await socket.ConnectAsync(new Uri($"{_baseUrl}"), cancellationToken);
            return socket;
        }

        internal OrderGuard AcquireOrderGuard()
        {
            return new OrderGuard(_pendingOrders, _mutexes);
        }

    }
}
