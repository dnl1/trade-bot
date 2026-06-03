using FluentScheduler;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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
        private readonly ConcurrentDictionary<long, TaskCompletionSource<OrderUpdateResult>> _orderCompletions;
        private readonly ConcurrentDictionary<string, Task> _subscribers;

        public BinanceStreamManager(BinanceApiClient binanceApiClient, ILogger logger)
        {
            _apiClient = binanceApiClient;
            _logger = logger;
            _baseUrl = "wss://stream.binance.com:9443/ws"; // primary endpoint — more stable than data-stream.binance.vision

            _pendingOrders = new ConcurrentDictionary<long, OrderUpdateResult>();
            _orderCompletions = new ConcurrentDictionary<long, TaskCompletionSource<OrderUpdateResult>>();
            _subscribers = new ConcurrentDictionary<string, Task>();
        }

        public void Subscribe<T>(IEnumerable<string> tickers, string streamName, Action<T> action, CancellationToken cancellationToken)
        {
            string subscriberId = $"{nameof(Subscribe)} {streamName}";

            var task = Task.Run(async () =>
            {
                int attempt = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using var socket = await CreateSocket(cancellationToken);

                        await Send(socket, new
                        {
                            method = "SUBSCRIBE",
                            @params = tickers.Select(ticker => $"{ticker.ToLower()}@{streamName}"),
                            id = 1
                        }, cancellationToken);

                        // Drain the subscription-confirmation frame
                        using var confirmMs = new MemoryStream();
                        var confirmBuf = new byte[8192];
                        WebSocketReceiveResult confirmResult;
                        do
                        {
                            confirmResult = await socket.ReceiveAsync(new ArraySegment<byte>(confirmBuf), cancellationToken);
                            confirmMs.Write(confirmBuf, 0, confirmResult.Count);
                        } while (!confirmResult.EndOfMessage);

                        attempt = 0; // reset on successful connect
                        await Listen(socket, subscriberId, rcvMsg =>
                        {
                            var obj = JsonConvert.DeserializeObject<T>(rcvMsg);
                            if (null != obj) action(obj);
                        }, cancellationToken);

                        // Listen returned normally (graceful Close frame) — brief delay before reconnect
                        _logger.Info($"[Stream] '{subscriberId}' closed gracefully, reconnecting in 2s");
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
                    catch (OperationCanceledException) { /* reconnect-triggered cancel — loop again */ }
                    catch (Exception ex)
                    {
                        attempt++;
                        var delay = TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, attempt)));
                        // Binance drops connections every 24h or during maintenance — this is expected.
                        var level = attempt <= 2 ? "INFO" : "WARN";
                        if (attempt <= 2)
                            _logger.Info($"[Stream] '{subscriberId}' reconnecting in {delay.TotalSeconds}s (attempt {attempt}): {ex.Message}");
                        else
                            _logger.Warn($"[Stream] '{subscriberId}' repeated disconnect (attempt {attempt}), reconnecting in {delay.TotalSeconds}s: {ex.Message}");
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }, cancellationToken);

            _subscribers[subscriberId] = task;

            // Clean up completed/faulted tasks from previous runs so the dict doesn't grow unbounded
            foreach (var key in new List<string>(_subscribers.Keys))
                if (_subscribers.TryGetValue(key, out var t) && t.IsCompleted && key != subscriberId)
                    _subscribers.TryRemove(key, out _);
        }

        internal async Task StreamProcessor(CancellationToken cancellationToken)
        {
            int attempt = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using var socket = await CreateSocket(cancellationToken, "wss://ws-api.binance.com:443/ws-api/v3");

                    await Send(socket, new
                    {
                        id     = Guid.NewGuid().ToString("N"),
                        method = "userDataStream.subscribe.signature",
                        @params = _apiClient.CreateSignedWebSocketParameters()
                    }, cancellationToken);

                    attempt = 0; // reset on successful connect

                    // Separate CTS so serverShutdown can break Listen without stopping the whole bot
                    using var reconnectCts = new CancellationTokenSource();
                    using var linkedCts    = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, reconnectCts.Token);

                    await Listen(socket, nameof(StreamProcessor), rcvMsg =>
                    {
                        var payload = JsonConvert.DeserializeObject<JObject>(rcvMsg);
                        if (payload == null) return;

                        if (payload["status"] != null)
                        {
                            var status = payload["status"]!.Value<int>();
                            if (status >= 400)
                                _logger.Error($"User data stream subscription failed: {payload}");
                            else if (payload["result"]?["subscriptionId"] != null)
                                _logger.Info($"User data stream subscribed: {payload["result"]!["subscriptionId"]}");
                            return;
                        }

                        var eventPayload = payload["event"];
                        if (eventPayload == null) return;

                        if (eventPayload["e"]?.Value<string>() == "serverShutdown")
                        {
                            _logger.Warn("Binance signaled server shutdown — triggering reconnect");
                            reconnectCts.Cancel(); // breaks out of Listen cleanly
                            return;
                        }

                        var obj = eventPayload.ToObject<OrderUpdateResult>();
                        if (null != obj && obj.OrderId > 0)
                        {
                            _pendingOrders[obj.OrderId] = obj;
                            if (_orderCompletions.TryGetValue(obj.OrderId, out var tcs))
                                tcs.TrySetResult(obj);
                        }
                    }, linkedCts.Token);

                    // Listen returned or was canceled — brief delay before reconnect
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.Info("[StreamProcessor] Closed gracefully, reconnecting in 2s");
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
                catch (OperationCanceledException) { /* reconnect-triggered cancel — loop again */ }
                catch (Exception ex)
                {
                    attempt++;
                    var delay = TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, attempt)));
                    _logger.Warn($"[StreamProcessor] Disconnected (attempt {attempt}), reconnecting in {delay.TotalSeconds}s: {ex.Message}");
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        private static async Task Send(ClientWebSocket socket, object payload, CancellationToken cancellationToken)
        {
            string payloadStr = JsonConvert.SerializeObject(payload);
            var sndBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(payloadStr));

            await socket.SendAsync(sndBuffer, WebSocketMessageType.Text, true, cancellationToken);
        }

        private async Task Listen(ClientWebSocket socket, string socketName, Action<string> action, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            using var ms = new MemoryStream();

            while (true)
            {
                ms.SetLength(0);
                WebSocketReceiveResult rcvResult;

                do
                {
                    rcvResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (rcvResult.MessageType == WebSocketMessageType.Close)
                    {
                        // Binance often closes without completing the handshake — CloseAsync may throw; that's fine.
                        _logger.Info($"[{socketName}] Server closed stream — reconnecting");
                        try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None); }
                        catch { /* connection already gone */ }
                        return;
                    }

                    // Note: .NET ClientWebSocket handles WebSocket-level Ping/Pong automatically
                    // at the transport layer — ReceiveAsync never surfaces Ping frames to the app.
                    ms.Write(buffer, 0, rcvResult.Count);
                } while (!rcvResult.EndOfMessage);

                string rcvMsg = Encoding.UTF8.GetString(ms.ToArray());

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

        private async Task<ClientWebSocket> CreateSocket(CancellationToken cancellationToken, string? url = null)
        {
            url ??= _baseUrl;

            var socket = new ClientWebSocket();

            await socket.ConnectAsync(new Uri(url), cancellationToken);
            return socket;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal OrderGuard AcquireOrderGuard() =>
            new OrderGuard(_pendingOrders, _orderCompletions);

    }
}
