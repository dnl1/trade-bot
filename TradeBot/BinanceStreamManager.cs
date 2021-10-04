using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TradeBot.Settings;

namespace TradeBot
{
    internal class BinanceStreamManager
    {
        private readonly AppSettings _settings;
        private readonly string _baseUrl;
        private readonly Mutex _mutex;
        private readonly Dictionary<string, int> _pendingOrders;

        public BinanceStreamManager(AppSettings settings)
        {
            _settings = settings;
            _baseUrl = "wss://stream.binance.com:9443/ws";

            _mutex = new Mutex();
        }

        public void Subscribe(IEnumerable<string> tickers, Action<Snapshot> action, CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                var socket = new ClientWebSocket();

                await socket.ConnectAsync(new Uri($"{_baseUrl}"), cancellationToken);

                object payload = new
                {
                    method = "SUBSCRIBE",
                    @params = tickers.Select(ticker => $"{ticker.ToLower()}@aggTrade"),
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
                    var obj = JsonConvert.DeserializeObject<Snapshot>(rcvMsg);

                    if (null != obj)
                        action(obj);
                }
            },TaskCreationOptions.LongRunning);
        }

        internal OrderGuard AcquireOrderGuard()
        {
            return new OrderGuard(_pendingOrders, _mutex);
        }
    }
}
