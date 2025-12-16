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
    private static TimeSpan IntervalToSpan(KlineInterval i) => i switch
    {
        KlineInterval.OneMinute => TimeSpan.FromMinutes(1),
        KlineInterval.FiveMinutes => TimeSpan.FromMinutes(5),
        KlineInterval.FifteenMinutes => TimeSpan.FromMinutes(15),
        KlineInterval.OneHour => TimeSpan.FromHours(1),
        KlineInterval.FourHour => TimeSpan.FromHours(4),
        KlineInterval.OneDay => TimeSpan.FromDays(1),
        _ => TimeSpan.FromMinutes(1)
    };

    public string Name => $"Binance LIVE {_symbol} {_interval}";

    public BinanceLiveCandleFeed(BinanceDataSource ds, string symbol, KlineInterval interval)
    {
        _ds = ds;
        _symbol = symbol;
        _interval = interval;
    }

    public async Task<IReadOnlyList<Candle>> GetWarmupAsync(int warmupCount, CancellationToken ct)
    {
        // Binance зазвичай дає до 1000 limit, але нам достатньо
        var limit = Math.Min(500, Math.Max(warmupCount + 5, 200));
        var all = await _ds.GetRecentCandlesAsync(_symbol, _interval, limit, ct);
        // прибрати останню "поточну" (якщо вона ще формується)
        var span = IntervalToSpan(_interval);
        var now = DateTime.UtcNow;

        if (all.Count > 0 && all[^1].Time + span > now.AddSeconds(-2))
            all.RemoveAt(all.Count - 1);

        return all.TakeLast(warmupCount).ToList();
    }

    public async IAsyncEnumerable<Candle> StreamAsync([EnumeratorCancellation] CancellationToken ct)
    {
        // прайм
        DateTime? lastEmittedTime = null;
        try
        {
            var prime = await _ds.GetRecentCandlesAsync(_symbol, _interval, 3, ct);
            if (prime.Count >= 2)
                lastEmittedTime = prime[^2].Time;
        }
        catch { /* ігноруємо */ }
        while (!ct.IsCancellationRequested)
        {
            Candle? toYield = null;

            try
            {
                var candles = await _ds.GetRecentCandlesAsync(_symbol, _interval, 3, ct);

                Console.WriteLine($"[POLL] got={candles.Count} utc={DateTime.UtcNow:HH:mm:ss}");

                if (candles.Count >= 2)
                {
                    var closed = candles[^2];

                    if (lastEmittedTime is null || closed.Time > lastEmittedTime.Value)
                    {
                        lastEmittedTime = closed.Time;
                        Console.WriteLine($"[YIELD] closed={closed.Time:yyyy-MM-dd HH:mm}");
                        toYield = closed;
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

            if (toYield is not null)
                yield return toYield.Value;


            var span = IntervalToSpan(_interval);

            var delay = TimeSpan.FromSeconds(5); // дефолт на випадок, якщо ще нічого не емітили
            if (lastEmittedTime is not null)
            {
                var target = lastEmittedTime.Value + span + span + TimeSpan.FromSeconds(2);
                delay = target - DateTime.UtcNow;

                if (delay < TimeSpan.FromSeconds(2))
                    delay = TimeSpan.FromSeconds(2);
            }

            await Task.Delay(delay, ct);
        }
    }

}