# Trade Bot — Developer Guide

C# .NET 5.0 cryptocurrency trading bot for Binance. Automates coin swapping based on price ratio thresholds across a configured coin list. Inspired by [binance-trade-bot](https://github.com/edeng23/binance-trade-bot).

## Quick Start

```bash
# Copy and fill config
cp TradeBot/appsettings.Example.json TradeBot/appsettings.json
# edit appsettings.json with your Binance API keys and coins

# Run locally
dotnet run --project TradeBot/

# Run via Docker
docker-compose build && docker-compose up
```

## Configuration (`appsettings.json`)

All keys are lowercase in JSON. Bound to `AppSettings` in `TradeBot/Settings/AppSettings.cs`.

| Key | Type | Description |
|-----|------|-------------|
| `apikey` | string | Binance API key |
| `apisecretkey` | string | Binance secret key |
| `bridge` | string | Base currency (e.g. `"USDT"`) |
| `tld` | string | Binance domain suffix (`"com"`, `"us"`, `"co.in"`) |
| `currentcoin` | string | Starting coin (leave empty for random) |
| `scoutMultiplier` | decimal | Fee weight multiplier — higher = more conservative (default `5`) |
| `scoutSleepTime` | int | Minutes between scout runs (default `1`) |
| `buyTimeout` | int | Minutes before canceling an unfilled buy (`0` = never) |
| `sellTimeout` | int | Minutes before canceling an unfilled sell (`0` = never) |
| `strategy` | string | `"default"` (single coin) or `"multipleCoins"` (all coins) |
| `coins` | string[] | Coins to trade, e.g. `["BTC","ETH","SOL"]` |
| `telegramBotId` | string | Telegram bot token |
| `telegramChatId` | string | Telegram chat ID |
| `loggers` | string[] | Active sinks: `["console"]`, `["console","telegram"]` |

## Running Tests

```bash
dotnet test TradeBot.Tests/
```

Tests use xUnit + FluentAssertions. Test files live in `TradeBot.Tests/Entities/`.

## Build & Publish

```bash
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

## Project Structure

```
TradeBot/
├── Program.cs                  # DI setup and entry point
├── BinanceApiManager.cs        # Core buy/sell logic, order lifecycle
├── BinanceStreamManager.cs     # WebSocket subscriptions, user data stream
├── OrderGuard.cs               # Thread sync between HTTP orders and WS updates
├── Entities/                   # Domain types: Coin, Pair, Trade
├── Enums/                      # Side (BUY/SELL), TradeState
├── Database/                   # IDatabase<T>, InMemoryDatabase<T>, ICacher/Cacher
├── Models/                     # API response shapes: Account, ExchangeInfo, Snapshot
├── Factories/                  # StrategyFactory
├── HostedServices/             # TradingService (main loop), MarketDataListenerService
├── Repositories/               # BinanceApiClient (HTTP), CoinRepository, PairRepository, etc.
├── Services/                   # ITradeService / TradeService (audit log)
├── Strategies/                 # AutoTrader (base), DefaultStrategy, MultipleCoinsStrategy
├── Logger/                     # Custom multi-sink logger
│   └── Sinks/Console|Telegram/
├── Converters/                 # DecimalConverter for JSON
└── Settings/                   # AppSettings.cs

TradeBot.Tests/
├── Entities/                   # Unit tests for Coin, Trade
└── Builders/Entities/          # CoinBuilder for test setup
```

## Architecture Overview

```
Program.cs
    └── TradingService (IHostedService)
            ├── MarketDataListenerService   ← WebSocket price feed (aggTrade per pair)
            ├── BinanceApiManager           ← Places orders, waits for fills
            │       └── BinanceApiClient   ← REST API (signed HMAC-SHA256)
            ├── BinanceStreamManager        ← WebSocket user data stream (order updates)
            │       └── OrderGuard         ← ManualResetEventSlim sync primitive
            ├── Strategy (AutoTrader)       ← Scout loop via FluentScheduler
            │       ├── DefaultStrategy
            │       └── MultipleCoinsStrategy
            └── Logger                     ← ConsoleSink + TelegramSink
```

## Key Flows

### Startup
1. Save configured coins to `CoinRepository`
2. Start `MarketDataListenerService` (opens WebSocket to `aggTrade` stream)
3. Wait for all initial snapshots (via `CountdownEvent`)
4. Call `strategy.Initialize()` — computes pair ratios from first snapshot batch
5. Run first scout, then schedule recurring scouts via `FluentScheduler`

### Scout → Trade
1. Scout evaluates: `profit = (price_A/price_B) - fees*ScoutMultiplier - storedRatio`
2. If `profit > 0`, executes: Sell A → USDT → Buy B
3. Each order uses `OrderGuard`: places HTTP order, blocks on `ManualResetEventSlim`, unblocked by WebSocket update in `BinanceStreamManager`
4. After fill: updates stored pair ratio and current coin

### Order Timeout
- **Buy**: if NEW for `> buyTimeout` min OR if PARTIALLY_FILLED and current price > limit by 0.1% → cancel
- **Sell**: if PARTIALLY_FILLED for `> sellTimeout` min → cancel

## Caching

`Cacher` (in `TradeBot/Database/`) caches by `typeof(T)` with a TTL:
- Exchange symbol info → **12 hours**
- Trade fees → **12 hours**
- BNB burn status → **60 seconds**

## Logging

Custom `ILogger` (not Microsoft's) broadcasts `LogEvent` to all registered sinks.

| Sink | Output | Levels |
|------|--------|--------|
| `ConsoleSink` | stdout with colors | All (Debug/Info/Warn/Error) |
| `TelegramSink` | Telegram bot message | Info, Warn, Error only |

`TelegramSink` uses an async queue — network I/O never blocks the trading loop.

## HTTP Resilience

`BinanceApiClient` uses Polly with exponential backoff: up to **20 retries**, doubling sleep each attempt. Configured in `Program.cs` via `AddPolicyHandler`.

## Adding a New Strategy

1. Create `TradeBot/Strategies/YourStrategy.cs` extending `AutoTrader`
2. Override `Scout()` and optionally `Initialize()`
3. Add a case to `StrategyFactory.cs`
4. Set `"strategy": "yourStrategy"` in `appsettings.json`

## Adding a New Log Sink

1. Implement `ILogEventSink` in `TradeBot/Logger/Sinks/YourSink/`
2. Add an extension method on `LoggerBuilder` (see `ConsoleSinkExtensions.cs`)
3. Register it in `ServiceCollectionExtensions.cs` based on the logger name string

## Extending Data Persistence

Current implementation: `InMemoryDatabase<T>` (volatile, lost on restart). The README notes a real database as a planned next step.

To swap: implement `IDatabase<T>` with EF Core or Dapper, then change the DI registration in `Program.cs` from `InMemoryDatabase<T>` to your implementation.

## Deployment

### Docker
```bash
docker-compose build
docker-compose up -d
```

### Azure Container
See the [Microsoft docs linked in README](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal) for container registry setup.

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `FluentScheduler` | 5.5.1 | Recurring scout job |
| `Microsoft.Extensions.Hosting` | 5.0.0 | IHostedService, DI container |
| `Microsoft.Extensions.Http.Polly` | 5.0.1 | HTTP retry policies |
| `Newtonsoft.Json` | 13.0.1 | JSON deserialization (custom converters) |
| `Polly` | 7.2.2 | Resilience & retry |
| `Telegram.Bot` | 16.0.2 | Telegram notifications |
| `System.Net.Http` | 4.3.4 | HTTP client primitives |

Test project uses `xunit` and `FluentAssertions`.
