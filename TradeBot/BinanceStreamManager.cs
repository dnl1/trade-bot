using FluentScheduler;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
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
        private readonly BinanceApiClient _apiClient;
        private readonly ILogger _logger;
        private readonly string _baseUrl;
        private readonly ConcurrentDictionary<long, OrderUpdateResult> _pendingOrders;
        private readonly CancellationToken _cancellationToken;
        private readonly Dictionary<long, ManualResetEventSlim> _mutexes;
        private readonly Dictionary<string, Thread> _subscribers;

        public BinanceStreamManager(BinanceApiClient binanceApiClient, ILogger logger)
        {
            _apiClient = binanceApiClient;
            _logger = logger;
            _baseUrl = "wss://stream.binance.com:9443/ws";
            _cancellationToken = new CancellationTokenSource(5000).Token;

            _mutexes = new Dictionary<long, ManualResetEventSlim>();
            _pendingOrders = new ConcurrentDictionary<long, OrderUpdateResult>();
            _subscribers = new Dictionary<string, Thread>();
        }

        public void Subscribe<T>(IEnumerable<string> tickers, string streamName, Action<T> action, CancellationToken cancellationToken)
        {
            string subscriberId = $"{nameof(Subscribe)} {streamName}";

            var thread = new Thread(async () =>
            {
                ClientWebSocket socket = await CreateSocket();

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
                WebSocketReceiveResult pingResult = await socket.ReceiveAsync(rcvBuffer, cancellationToken);

                await Listen(socket, nameof(Subscribe), (rcvMsg) =>
                {
                    var obj = JsonConvert.DeserializeObject<T>(rcvMsg);

                    if (null != obj)
                        action(obj);
                }, cancellationToken);
            })
            {
                Name = subscriberId
            };

            _subscribers.Add(subscriberId, thread);

            _subscribers[subscriberId].Start();
        }

        internal async Task StreamProcessor(CancellationToken cancellationToken)
        {
            var listenKey = await _apiClient.GetListenKey();

            JobManager.AddJob(() => _apiClient.GetListenKey().Wait(), s => s.WithName(nameof(StreamProcessor)).ToRunEvery(30).Minutes());
            ClientWebSocket socket = await CreateSocket($"{_baseUrl}/{listenKey.ListenKey}");

            await Listen(socket, nameof(StreamProcessor), (rcvMsg) =>
            {
                var obj = JsonConvert.DeserializeObject<OrderUpdateResult>(rcvMsg);

                if (null != obj && obj.OrderId > 0)
                {
                    _pendingOrders[obj.OrderId] = obj;

                    if (_mutexes.ContainsKey(obj.OrderId))
                        _mutexes[obj.OrderId].Set();
                }
            }, cancellationToken);
        }

        private async Task Listen(ClientWebSocket socket, string socketName, Action<string> action, CancellationToken cancellationToken)
        {
            var rcvBytes = new byte[1000];
            var rcvBuffer = new ArraySegment<byte>(rcvBytes);

            while (true)
            {
                WebSocketReceiveResult rcvResult = await socket.ReceiveAsync(rcvBuffer, cancellationToken);

                if (rcvResult.MessageType.Equals(WebSocketMessageType.Close))
                {
                    _logger.Warn($"Socket {socketName} closed tcp connection");
                }

                byte[] msgBytes = rcvBuffer.Skip(rcvBuffer.Offset).Take(rcvResult.Count).ToArray();
                string rcvMsg = Encoding.UTF8.GetString(msgBytes);

                try
                {
                    action(rcvMsg);
                }
                catch (Exception ex)
                {
                    _logger.Error($"{socketName} {ex}");
                }
            }
        }

        private async Task<ClientWebSocket> CreateSocket(string url = null)
        {
            url ??= _baseUrl;

            var socket = new ClientWebSocket();

            await socket.ConnectAsync(new Uri(url), _cancellationToken);
            return socket;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal OrderGuard AcquireOrderGuard() =>
            new OrderGuard(_pendingOrders, _mutexes);

    }
}
