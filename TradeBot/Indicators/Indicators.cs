using System;
using System.Collections.Generic;
using System.Linq;
using TradeBot.Models;

namespace TradeBot.Indicators
{
    public record BbResult(decimal Upper, decimal Middle, decimal Lower, decimal Bandwidth, decimal PercentB);

    public static class Indicators
    {
        // ── Bollinger Bands ───────────────────────────────────────────────────────
        public static BbResult? BollingerBands(IReadOnlyList<Candle> candles, int period = 20, decimal multiplier = 2.0m)
        {
            if (candles.Count < period) return null;

            var closes = candles.TakeLast(period).Select(c => c.Close).ToList();
            var sma    = closes.Average();
            var variance = closes.Sum(c => (c - sma) * (c - sma)) / period;
            var std    = (decimal)Math.Sqrt((double)variance);

            var upper = sma + multiplier * std;
            var lower = sma - multiplier * std;
            var range = upper - lower;

            var bandwidth = sma > 0 ? range / sma : 0m;
            var pctB      = range > 0 ? (candles[^1].Close - lower) / range : 0.5m;

            return new BbResult(upper, sma, lower, bandwidth, pctB);
        }

        // ── RSI (Wilder smoothing) ─────────────────────────────────────────────
        public static decimal? Rsi(IReadOnlyList<Candle> candles, int period = 14)
        {
            if (candles.Count < period + 1) return null;

            var closes = candles.Select(c => c.Close).ToList();

            decimal avgGain = 0, avgLoss = 0;
            for (int i = 1; i <= period; i++)
            {
                var delta = closes[i] - closes[i - 1];
                avgGain += Math.Max(delta, 0);
                avgLoss += Math.Max(-delta, 0);
            }
            avgGain /= period;
            avgLoss /= period;

            for (int i = period + 1; i < closes.Count; i++)
            {
                var delta = closes[i] - closes[i - 1];
                avgGain = (avgGain * (period - 1) + Math.Max(delta, 0)) / period;
                avgLoss = (avgLoss * (period - 1) + Math.Max(-delta, 0)) / period;
            }

            if (avgLoss == 0) return 100m;
            return 100m - 100m / (1m + avgGain / avgLoss);
        }

        // ── ATR (Wilder smoothing) ─────────────────────────────────────────────
        public static decimal? Atr(IReadOnlyList<Candle> candles, int period = 14)
        {
            if (candles.Count < period + 1) return null;

            var trs = TrueRanges(candles);

            var atr = trs.Take(period).Average();
            foreach (var tr in trs.Skip(period))
                atr = (atr * (period - 1) + tr) / period;

            return atr;
        }

        // Rolling mean of ATR — used to normalise current ATR against recent baseline.
        public static decimal? AtrSma(IReadOnlyList<Candle> candles, int atrPeriod = 14, int smaPeriod = 20)
        {
            // Minimum candles: enough to compute ATR at each of the last smaPeriod points
            int needed = atrPeriod + smaPeriod + 1;
            if (candles.Count < needed) return null;

            var trs    = TrueRanges(candles).ToList();
            var window = new decimal[smaPeriod];

            // Seed ATR with the first atrPeriod TRs
            var atr = trs.Take(atrPeriod).Average();
            int slot = 0;

            for (int i = atrPeriod; i < trs.Count; i++)
            {
                atr = (atr * (atrPeriod - 1) + trs[i]) / atrPeriod;
                if (i >= trs.Count - smaPeriod)
                    window[slot++] = atr;
            }

            return window.Average();
        }

        // ── ADX ───────────────────────────────────────────────────────────────
        public static decimal? Adx(IReadOnlyList<Candle> candles, int period = 14)
        {
            if (candles.Count < period * 2 + 1) return null;

            var plusDM  = new List<decimal>(candles.Count);
            var minusDM = new List<decimal>(candles.Count);
            var trs     = new List<decimal>(candles.Count);

            for (int i = 1; i < candles.Count; i++)
            {
                var up   = candles[i].High - candles[i - 1].High;
                var down = candles[i - 1].Low - candles[i].Low;
                plusDM .Add(up   > down && up   > 0 ? up   : 0);
                minusDM.Add(down > up   && down > 0 ? down : 0);

                var hl = candles[i].High - candles[i].Low;
                var hc = Math.Abs(candles[i].High - candles[i - 1].Close);
                var lc = Math.Abs(candles[i].Low  - candles[i - 1].Close);
                trs.Add(Math.Max(hl, Math.Max(hc, lc)));
            }

            // Wilder-smooth TR, +DM, -DM
            decimal sTr    = trs    .Take(period).Sum();
            decimal sPlus  = plusDM .Take(period).Sum();
            decimal sMinus = minusDM.Take(period).Sum();

            var dxList = new List<decimal>();

            for (int i = period; i < trs.Count; i++)
            {
                sTr    = sTr    - sTr    / period + trs    [i];
                sPlus  = sPlus  - sPlus  / period + plusDM [i];
                sMinus = sMinus - sMinus / period + minusDM[i];

                var pDI    = sTr > 0 ? 100m * sPlus  / sTr : 0m;
                var mDI    = sTr > 0 ? 100m * sMinus / sTr : 0m;
                var diSum  = pDI + mDI;
                dxList.Add(diSum > 0 ? 100m * Math.Abs(pDI - mDI) / diSum : 0m);
            }

            if (dxList.Count < period) return null;

            var adx = dxList.Take(period).Average();
            foreach (var dx in dxList.Skip(period))
                adx = (adx * (period - 1) + dx) / period;

            return adx;
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static IEnumerable<decimal> TrueRanges(IReadOnlyList<Candle> candles)
        {
            for (int i = 1; i < candles.Count; i++)
            {
                var hl = candles[i].High - candles[i].Low;
                var hc = Math.Abs(candles[i].High - candles[i - 1].Close);
                var lc = Math.Abs(candles[i].Low  - candles[i - 1].Close);
                yield return Math.Max(hl, Math.Max(hc, lc));
            }
        }
    }
}
