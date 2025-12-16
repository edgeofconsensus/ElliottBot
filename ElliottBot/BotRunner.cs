using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ElliottBot;

public sealed class BotRunner
{
    private readonly ElliottBot _bot;
    private readonly int _warmupCount;

    public BotRunner(ElliottBot bot, int warmupCount = 200)
    {
        _bot = bot;
        _warmupCount = warmupCount;
    }

    public async Task RunAsync(ICandleFeed feed, CancellationToken ct)
    {
        Console.WriteLine($"=== FEED: {feed.Name} ===");

        var history = new List<Candle>(await feed.GetWarmupAsync(_warmupCount, ct));
        Console.WriteLine($"Warmup candles: {history.Count}");

        await foreach (var candle in feed.StreamAsync(ct))
        {
            Console.WriteLine($"[CANDLE] {candle.Time:yyyy-MM-dd HH:mm} O={candle.Open} H={candle.High} L={candle.Low} C={candle.Close}");

            // важливо: history НЕ містить поточну свічку
            _bot.OnNewCandle(history, candle);

            history.Add(candle);

            // опційно: щоб history не росло безкінечно
            if (history.Count > 2000)
                history.RemoveRange(0, history.Count - 2000);
        }
    }
}
