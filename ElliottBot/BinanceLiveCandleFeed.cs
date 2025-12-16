using Binance.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ElliottBot;

public sealed class BinanceLiveCandleFeed : ICandleFeed
{
    private readonly BinanceDataSource _ds;
    private readonly string _symbol;
    private readonly KlineInterval _interval;

    public string Name => $"Binance LIVE {_symbol} {_interval}";

    public BinanceLiveCandleFeed(BinanceDataSource ds, string symbol, KlineInterval interval)
    {
        _ds = ds;
        _symbol = symbol;
        _interval = interval;
    }

    public async Task<IReadOnlyList<Candle>> GetWarmupAsync(int warmupCount, CancellationToken ct)
    {
        // беремо трохи більше і обрізаємо
        var end = DateTime.UtcNow;
        var start = end.AddDays(-60); // для 1h це ок; можна зробити розумніше

        var all = await _ds.GetHistoricalCandlesAsync(_symbol, _interval, start, end);
        return all.TakeLast(warmupCount).ToList();
    }

    public async IAsyncEnumerable<Candle> StreamAsync([EnumeratorCancellation] CancellationToken ct)
    {
        DateTime? lastEmittedOpenTime = null;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var end = DateTime.UtcNow;
                var start = end.AddDays(-2);
                var candles = await _ds.GetHistoricalCandlesAsync(_symbol, _interval, start, end);
                Console.WriteLine($"[POLL] got={candles.Count} utc={DateTime.UtcNow:HH:mm:ss}");

                if (candles.Count >= 2)
                {
                    // ОСТАННЯ може бути ще незакрита, тому беремо передостанню як "closed"
                    var closed = candles[^2];

                    if (lastEmittedOpenTime is null || closed.Time > lastEmittedOpenTime.Value)
                    {
                        lastEmittedOpenTime = closed.Time;
                        Console.WriteLine($"[YIELD] closed={closed.Time:yyyy-MM-dd HH:mm}");
                        yield return closed;
                    }
                    else
                    {
                        Console.WriteLine($"[WAIT] lastClosed={closed.Time:yyyy-MM-dd HH:mm}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FEED ERROR] {ex.Message}");
            }

            // для 1h достатньо раз на хвилину/30с, щоб не спамити API
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }
}
