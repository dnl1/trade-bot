# AGENTS.md — AI Agent Guide for Trade Bot

This document provides the context an AI agent needs to work effectively on this codebase. Read `CLAUDE.md` first for the developer overview.

## What This Project Does

A Binance crypto trading bot that continuously scouts price ratios between configured coins and executes swaps when a profitable threshold is crossed. The strategy is: sell the coin you hold into a bridge (USDT), then buy the target coin, whenever the ratio between them has shifted enough to cover fees with margin.

The "enough margin" is: `(price_A / price_B) - (totalFee × ScoutMultiplier) > storedRatio`

Default ScoutMultiplier is 5, meaning fees are weighted 5× for conservatism.

## Critical Files to Read First

When working on any feature, read these files to understand the domain before touching anything:

| File | Why |
|------|-----|
| [TradeBot/BinanceApiManager.cs](TradeBot/BinanceApiManager.cs) | All order placement, fee calculation, quantity rounding |
| [TradeBot/BinanceStreamManager.cs](TradeBot/BinanceStreamManager.cs) | WebSocket handling, `_pendingOrders` dict, `OrderGuard` lifecycle |
| [TradeBot/OrderGuard.cs](TradeBot/OrderGuard.cs) | The sync primitive connecting HTTP orders to WS confirmations |
| [TradeBot/Strategies/AutoTrader.cs](TradeBot/Strategies/AutoTrader.cs) | Base strategy: `Scout()`, `Initialize()`, `GetRatios()`, `TransactionThroughBridge()` |
| [TradeBot/HostedServices/TradingService.cs](TradeBot/HostedServices/TradingService.cs) | Startup orchestration and scheduling |

## Data Flow (Read → Understand → Edit)

```
WebSocket aggTrade stream
    → BinanceStreamManager.Subscribe()
    → SnapshotRepository.Save(snapshot)       ← latest price for each pair

FluentScheduler timer
    → AutoTrader.Scout()
    → GetRatios(coin, currentPrice)           ← compares against stored Pair.Ratio
    → TransactionThroughBridge(pair)
    → BinanceApiManager.SellAlt()
        → BinanceApiClient.OrderLimitSell()   ← HTTP POST /v3/order
        → OrderGuard.Wait()                   ← blocks here
        → (WebSocket fires) StreamProcessor() → OrderGuard signaled
        → poll until FILLED / CANCELED
    → BinanceApiManager.BuyAlt()              ← same pattern
    → PairRepository.Save(updatedPair)        ← ratio updated to new threshold
```

## Thread Safety Invariants

These must be preserved when editing concurrent code:

1. `InMemoryDatabase<T>` locks on `Save()` but not `GetByKey()` — reads are unsynchronized. Safe because the snapshot repository overwrites atomically and readers only need the latest value.
2. `_pendingOrders` in `BinanceStreamManager` is a `ConcurrentDictionary` — safe for concurrent add/remove.
3. `_mutexes` is a plain `Dictionary` — only accessed from within `OrderGuard` lifecycle which is single-threaded per order. Do not add concurrent access without adding a lock.
4. `OrderGuard` is `IDisposable` — always use `using`. Disposing removes the order from `_pendingOrders` and `_mutexes`. Leaking it starves the dict.
5. The `TelegramSink` queue runs on a dedicated background thread. Never await Telegram I/O inline in the trading loop.

## Quantity Calculation Rules (Important for Correctness)

Binance's `LOT_SIZE` filter requires quantities to be multiples of `stepSize`. The current logic:

```csharp
// stepSize example: "0.00001000" → 5 decimal places
int decimals = stepSize.IndexOf('1') - stepSize.IndexOf('.') - 1;
decimal qty = Math.Floor(rawQty * (decimal)Math.Pow(10, decimals))
              / (decimal)Math.Pow(10, decimals);
```

If you change quantity calculation anywhere, you must preserve LOT_SIZE rounding or Binance will reject the order with `-1013 LOT_SIZE`.

## API Signing

All authenticated endpoints go through `BinanceApiClient`. The signing flow:
1. Add `timestamp` (Unix ms) to params
2. Concatenate all params as query string
3. HMAC-SHA256(secretKey, queryString) → `signature` appended to URL
4. API key goes in header `X-MBX-APIKEY`

Never add raw `timestamp` or `signature` to requests manually — `BinanceApiClient` handles this in `SendSigned*()` helpers.

## Repository Pattern Conventions

All repositories use `InMemoryDatabase<T>` keyed by string:

| Repository | Key format | Example |
|------------|-----------|---------|
| `CoinRepository` | `"CURRENT"` (current) or symbol | `"BTC"` |
| `PairRepository` | `"FROM vs TO"` | `"BTC vs ETH"` |
| `SnapshotRepository` | pair symbol | `"BTCUSDT"` |
| `TradeRepository` | auto-increment string | `"1"`, `"2"`, … |

When adding a new repository, follow this key scheme and register in `Program.cs` as `IDatabase<YourEntity>` → `InMemoryDatabase<YourEntity>` (singleton).

## Strategy Extension Points

To add a strategy:
1. Extend `AutoTrader` in `TradeBot/Strategies/`
2. Override `Scout()` — this is called every `ScoutSleepTime` minutes
3. Optionally override `Initialize()` — called once after first snapshots arrive
4. Add a string → type mapping in `StrategyFactory.cs`
5. Set `"strategy": "yourKey"` in `appsettings.json`

`AutoTrader.GetRatios()` and `TransactionThroughBridge()` are reusable — prefer calling them over duplicating the fee/ratio logic.

## Logger Usage

The project uses a custom `ILogger` (not `Microsoft.Extensions.Logging.ILogger`). Inject `TradeBot.Logger.ILogger`.

```csharp
_logger.Debug("Internal only — skipped by TelegramSink");
_logger.Info("Trade started: BTC → ETH");
_logger.Warn("Order timed out, canceling");
_logger.Error("Failed to fetch balance");
```

Never block on Telegram delivery — the sink is async and queued internally.

## Adding a Log Sink

1. Create `TradeBot/Logger/Sinks/YourSink/YourSink.cs` implementing `ILogEventSink`
2. Add extension method on `LoggerBuilder` (follow `ConsoleSinkExtensions.cs`)
3. In `ServiceCollectionExtensions.cs`, add your sink name to the switch that reads `appSettings.Loggers`

## Caching Pattern

Use `ICacher.ExecuteAsync<T>(async () => await fetch(), ttl)` for any API call that returns stable data:

```csharp
var info = await _cacher.ExecuteAsync(
    () => _client.GetSymbolInfo(symbol),
    TimeSpan.FromHours(12));
```

`Cacher` keys by `typeof(T)` — one cached value per type. If you need per-symbol caching, you'll need to extend `ICacher` or pass a key parameter.

## Common Pitfalls

### Decimal Precision
Binance returns prices/quantities as JSON strings in some endpoints and floats in others. `DecimalConverter` handles this — do not remove it from `JsonSerializerSettings`. Any new deserialized API model that includes price/qty fields must use the global settings (they're applied in `BinanceApiClient`).

### Null Snapshot
`GetTickerPrice(symbol)` returns `decimal?`. If a coin's WebSocket stream hasn't delivered a snapshot yet, it returns `null`. Strategy code guards against this — preserve null-checks when modifying `Scout()`.

### BNB Fee Discount
`BinanceApiManager.GetFee()` checks whether BNB burn is active on the account. If active, taker fee is multiplied by `0.75`. This affects the profit threshold — if you change the fee logic, verify it against both burn-on and burn-off paths.

### Order May Not Fill
The order polling loop in `BinanceApiManager` is time-bounded by `BuyTimeout`/`SellTimeout`. If a timeout triggers:
- On BUY: order is canceled via `CancelOrder()`, method returns `null`
- On SELL: order is canceled, method returns `null`

Callers in `AutoTrader` already handle `null` returns — preserve that null-check pattern in any new strategy code.

### Stream Reconnection
`BinanceStreamManager` does not currently implement automatic WebSocket reconnection. If the stream drops, the bot stops receiving prices/order updates silently. This is a known gap (in-memory DB and no reconnect logic are both called out in the README as future work).

## Testing Conventions

- Test files go in `TradeBot.Tests/Entities/` or appropriate subfolder
- Use `CoinBuilder` (in `TradeBot.Tests/Builders/Entities/`) as the pattern for any builder needed in tests
- FluentAssertions for assertions: `result.Should().Be(expected)`
- No mocks of external services currently — tests cover pure domain logic only
- If you add integration tests that hit Binance, mark them with a trait and exclude from CI

## Project-Level Conventions

- **No null propagation on `AppSettings`**: config values are required; the app will throw early if missing
- **Strategies are singletons**: they hold mutable state (current coin, pair ratios). Do not make them transient
- **Repositories are singletons**: they wrap the in-memory dict; transient would lose data between requests
- **JSON keys in config are lowercase**: `AppSettings` properties map to lowercase JSON via default binder behavior
- **No async `void`**: all async paths return `Task`. The one exception is the Telegram queue processor which is a dedicated background thread, not an async method
