namespace TradeBot.Models
{
    public class Candle
    {
        public string Symbol { get; init; } = string.Empty;
        public long OpenTime  { get; init; }
        public long CloseTime { get; init; }
        public decimal Open  { get; init; }
        public decimal High  { get; set; }
        public decimal Low   { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
}
