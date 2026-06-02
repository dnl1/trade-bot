CREATE TABLE IF NOT EXISTS coins (
    key TEXT PRIMARY KEY,
    symbol TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS pairs (
    from_symbol TEXT NOT NULL,
    to_symbol TEXT NOT NULL,
    ratio NUMERIC NOT NULL,
    PRIMARY KEY (from_symbol, to_symbol)
);

CREATE TABLE IF NOT EXISTS snapshots (
    symbol TEXT PRIMARY KEY,
    event_type TEXT NOT NULL,
    event_time BIGINT NOT NULL,
    agg_trade_id BIGINT NOT NULL,
    price NUMERIC NOT NULL,
    quantity TEXT NOT NULL,
    first_trade_id BIGINT NOT NULL,
    last_trade_id BIGINT NOT NULL,
    trade_time BIGINT NOT NULL,
    is_market_maker BOOLEAN NOT NULL,
    ignore_flag BOOLEAN NOT NULL
);

CREATE TABLE IF NOT EXISTS trades (
    id BIGSERIAL PRIMARY KEY,
    alt_starting_balance NUMERIC NULL,
    crypto_starting_balance NUMERIC NULL,
    crypto_trading_amount NUMERIC NOT NULL,
    alt_trade_amount DOUBLE PRECISION NULL,
    state INTEGER NOT NULL,
    alt_coin_symbol TEXT NULL,
    crypto_coin_symbol TEXT NULL,
    side INTEGER NULL,
    trade_date TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
