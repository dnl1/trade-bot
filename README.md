# Trade Bot

[![.NET](https://github.com/dnl1/trade-bot/actions/workflows/build.yml/badge.svg)](https://github.com/dnl1/trade-bot/actions/workflows/build.yml)

Binance trading bot with Bollinger Bands mean-reversion strategy and automatic exchange stop-loss orders. Based on [Binance Trade Bot](https://github.com/edeng23/binance-trade-bot).

---

## Quick Start (Homelab / Server)

**Requirements:** Ubuntu/Debian with root access.

### 1 — Server setup (one-time)

```bash
# Installs Docker, clones repo to /opt/trade-bot, registers systemd service
curl -fsSL https://raw.githubusercontent.com/dnl1/trade-bot/main/scripts/setup-server.sh \
  | sudo bash
```

This installs: Docker Engine, Docker Compose v2, logrotate, and creates the `trade-bot` systemd service.

### 2 — Configure credentials

```bash
nano /opt/trade-bot/.env
```

```env
BINANCE_API_KEY=your_api_key
BINANCE_SECRET_KEY=your_secret          # or use RSA below
BINANCE_RSA_KEY_PATH=/opt/trade-bot/binance_key.pem  # optional

POSTGRES_PASSWORD=strong_password

TELEGRAM_BOT_ID=bot_token
TELEGRAM_CHAT_ID=your_chat_id
```

### 3 — Start

```bash
sudo systemctl start trade-bot
sudo systemctl status trade-bot
```

The service is enabled on boot automatically by the setup script.

---

## Local Development

```bash
git clone https://github.com/dnl1/trade-bot.git
cd trade-bot

# Create .env with your credentials
cp .env.example .env
nano .env

# Build and start everything (postgres + migrations + bot)
make deploy

# Follow logs
make logs
```

Local requirements: Docker, Docker Compose v2, .NET 8 SDK (for tests only).

---

## Useful Commands

```bash
make setup        # create .env + logs folder
make deploy       # build + docker compose up
make logs         # real-time bot logs
make logs-file    # today's log file
make status       # container status
make restart      # restart bot only
make down         # stop everything
make update       # git pull + redeploy
make test         # run unit tests
```

On the server, equivalent via systemd:

```bash
sudo systemctl start|stop|restart|status trade-bot
journalctl -u trade-bot -f          # system logs
```

---

## Strategies

Configure via `"strategy"` in `appsettings.json`.

| Key | Class | Description |
|-----|-------|-------------|
| `default` | `DefaultStrategy` | Hold one coin, jump to the best ratio when profitable |
| `multipleCoins` | `MultipleCoinsStrategy` | Scan all configured coins in parallel |
| `bollingerBands` | `BollingerBandsStrategy` | Mean-reversion with BB + RSI + ADX + exchange stop-loss |

### Bollinger Bands

Recommended strategy. Enters when price is below the lower band with multiple quality filters and places a real `STOP_LOSS_LIMIT` order on the exchange after each buy.

**Warm-up:** needs 20 candles before emitting signals. With `candleTimeframeMinutes: 15`, this is ~5 hours.

**Entry filters (all must be true):**

| Filter | Description | Default |
|--------|-------------|---------|
| `expectedReturn > BbMinProfitAboveFees` | Expected profit covers fees + margin | 0.3% |
| `%B < 0` | Price below lower band | — |
| `RSI < RsiOversold` | Oversold | 30 |
| `ATR/ATR-SMA < AtrVolatilityMultiplier` | Normal volatility | 1.5× |
| `ADX < AdxThreshold` | No strong trend | 25 |
| `Bandwidth > BbMinBandwidth` | Bands not squeezed | 4% |

**Exit:** price reaches middle band (SMA) or stop-loss (`StopLossAtrMultiplier × ATR`).

---

## Configuration (`appsettings.json`)

Credentials go in `.env` — `appsettings.json` only holds behavioral settings.

| Key | Required | Description |
|-----|----------|-------------|
| `bridge` | yes | Base coin — usually `USDT` |
| `tld` | yes | `com` for Binance mainnet |
| `strategy` | yes | `bollingerBands` recommended |
| `coins` | yes | Array of symbols, e.g. `["BTC","ETH","SOL"]` |
| `scoutSleepTime` | yes | Scout interval in minutes (e.g. `5`) |
| `buyTimeout` / `sellTimeout` | no | Minutes before canceling stuck order |
| `loggers` | no | `["console", "telegram", "file"]` |
| `logFilePath` | no | Log directory (default: `/app/logs`) |
| `candleTimeframeMinutes` | no | Candle size in minutes (default: `15`) |

---

## Logging

Three sinks available — enable via `"loggers"` in appsettings:

| Sink | Format | Minimum level |
|------|--------|---------------|
| `console` | `2026-01-15 14:30:00 UTC [INFO ] message` | Debug |
| `file` | Daily file at `logs/tradebot-yyyy-MM-dd.log` | Debug |
| `telegram` | Emoji + timestamp + Markdown | Info |

---

## Data Flow

```
WebSocket aggTrade stream
    → BinanceStreamManager          ← auto-reconnect with exponential backoff
    → SnapshotRepository            ← latest price per symbol
    → CandleAggregator              ← aggregates trades into OHLCV candles

FluentScheduler (every ScoutSleepTime min)
    → Strategy.Scout()
    → BinanceApiManager.BuyAlt()    ← position sizing (BbPositionSizePct)
    → OrderGuard.WaitAsync()        ← TaskCompletionSource syncs HTTP ↔ WS
    → PlaceStopLoss()               ← STOP_LOSS_LIMIT on exchange after fill
```

---

## Persistence

PostgreSQL stores operational state and history.

| Table | Contents |
|-------|----------|
| `coins` | Configured coins + current coin |
| `pairs` | Scout thresholds |
| `snapshots` | Latest snapshot per symbol |
| `trades` | Trade log (append-only) |
| `candles` | Closed OHLCV candles |

Migrations run automatically on `docker compose up` via a one-shot container.

---

## Build & Tests

```bash
dotnet build -c Release
dotnet test
```

Current status: **0 warnings, 0 errors, 70/70 tests.**

---

## Upcoming Improvements

- Historical candle backfill (eliminate warm-up period)
- Performance dashboard and trade history
- Stop-loss monitoring thread independent of scout cycle
- Multiple simultaneous positions (MultipleCoins + BB)

---

## Architecture Notes

- **Thread safety:** `ConcurrentDictionary` for pending orders and order completions; single-threaded per `OrderGuard` lifecycle; no locks on snapshot reads
- **Order lifecycle:** HTTP POST → `OrderGuard.WaitAsync()` blocks → WebSocket fill event signals TCS → polling loop confirms FILLED/CANCELED
- **Startup safety:** fails fast on invalid strategy; 2-minute timeout for initial snapshots; 120-iteration guard on order polling
- **Shutdown:** host cancellation token propagates to WebSocket loops and Telegram queue processor
