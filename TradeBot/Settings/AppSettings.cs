namespace TradeBot.Settings
{
    public class AppSettings
    {
        public string ApiKey { get; set; } = null!;
        public string ApiSecretKey { get; set; } = null!;
        public string ApiPrivateKeyPath { get; set; } = null!;
        public string PostgresConnectionString { get; set; } = null!;
        public string Bridge { get; set; } = null!;
        public string Tld { get; set; } = null!;
        public string CurrentCoin { get; set; } = null!;
        public string TelegramBotId { get; set; } = null!;
        public string TelegramChatId { get; set; } = null!;
        public decimal ScoutMultiplier { get; set; }
        public int ScoutSleepTime { get; set; }
        // Max minutes a single Scout() call may run before it is considered hung and the lock released
        public int ScoutTimeoutMinutes { get; set; } = 10;
        public int BuyTimeout{ get; set; }
        public int SellTimeout { get; set; }
        public string Strategy { get; set; } = null!;
        public string[] Coins { get; set; } = System.Array.Empty<string>();
        public string[] Loggers { get; set; } = System.Array.Empty<string>();
        public string   LogFilePath { get; set; } = "/app/logs";

        // Bollinger Bands strategy settings
        public int     CandleTimeframeMinutes   { get; set; } = 240;   // 4H — validated by research
        public int     BbPeriod                 { get; set; } = 20;
        public decimal BbStdDev                 { get; set; } = 2.0m;
        public int     RsiPeriod                { get; set; } = 14;
        public decimal RsiOversold              { get; set; } = 30m;
        public decimal AdxThreshold             { get; set; } = 25m;
        public decimal AtrVolatilityMultiplier  { get; set; } = 1.5m;
        public decimal BbMinBandwidth           { get; set; } = 0.04m;
        public decimal StopLossAtrMultiplier    { get; set; } = 2.0m;
        // Minimum expected return above round-trip fees (2 × 0.1% taker = 0.2%)
        public decimal BbMinProfitAboveFees     { get; set; } = 0.003m; // 0.3% net minimum
        // Fraction of available bridge balance to deploy per trade
        public decimal BbPositionSizePct        { get; set; } = 0.95m;  // 95%
    }
}
