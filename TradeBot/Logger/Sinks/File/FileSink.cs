using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TradeBot.Logger;

namespace TradeBot
{
    /// <summary>
    /// Async file sink with daily rotation and per-flush batching.
    /// Drains the entire queue in one pass per iteration, grouping lines by
    /// date so only one file-open/write/close happens per day per flush cycle.
    /// Files: {LogDirectory}/tradebot-yyyy-MM-dd.log
    /// </summary>
    internal class FileSink : ILogEventSink, IDisposable
    {
        private readonly string _logDirectory;
        private readonly LogLevel _minLevel;
        private readonly ConcurrentQueue<LogEvent> _queue = new();
        private readonly CancellationTokenSource   _cts   = new();

        public FileSink(string logDirectory, LogLevel minLevel = LogLevel.Debug)
        {
            _logDirectory = logDirectory;
            _minLevel     = minLevel;
            Directory.CreateDirectory(logDirectory);
            _ = Task.Run(() => ProcessQueue(_cts.Token));
        }

        public void Emit(LogEvent evt) => _queue.Enqueue(evt);

        private async Task ProcessQueue(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Drain all pending events in one pass
                    var byDate = new Dictionary<string, StringBuilder>();

                    while (_queue.TryDequeue(out var evt))
                    {
                        if (evt.Level < _minLevel) continue;

                        var dateKey = evt.Timestamp.ToString("yyyy-MM-dd");
                        if (!byDate.TryGetValue(dateKey, out var sb))
                            byDate[dateKey] = sb = new StringBuilder();

                        sb.Append(evt.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                          .Append(" UTC [")
                          .Append(evt.Level.ToString().ToUpper().PadRight(5))
                          .Append("] ")
                          .AppendLine(evt.Message);
                    }

                    // One file-open per date per flush cycle.
                    // CancellationToken.None: we prefer a brief delay on shutdown over
                    // silently losing events that were already dequeued into byDate.
                    foreach (var (dateKey, sb) in byDate)
                    {
                        var path = Path.Combine(_logDirectory, $"tradebot-{dateKey}.log");
                        await File.AppendAllTextAsync(path, sb.ToString(), CancellationToken.None);
                    }

                    if (byDate.Count == 0)
                    {
                        try { await Task.Delay(200, ct); }
                        catch (OperationCanceledException) { break; }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[FileSink] Write error: {ex.Message}");
                    await Task.Delay(1000, CancellationToken.None); // brief pause before retry
                }
            }

            // Flush remaining events on shutdown (best-effort, no cancellation)
            await FlushRemaining();
        }

        private async Task FlushRemaining()
        {
            var byDate = new Dictionary<string, StringBuilder>();
            while (_queue.TryDequeue(out var evt))
            {
                if (evt.Level < _minLevel) continue;
                var dateKey = evt.Timestamp.ToString("yyyy-MM-dd");
                if (!byDate.TryGetValue(dateKey, out var sb))
                    byDate[dateKey] = sb = new StringBuilder();
                sb.Append(evt.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                  .Append(" UTC [").Append(evt.Level.ToString().ToUpper().PadRight(5))
                  .Append("] ").AppendLine(evt.Message);
            }
            foreach (var (dateKey, sb) in byDate)
            {
                try
                {
                    var path = Path.Combine(_logDirectory, $"tradebot-{dateKey}.log");
                    await File.AppendAllTextAsync(path, sb.ToString());
                }
                catch { /* best-effort on shutdown */ }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
