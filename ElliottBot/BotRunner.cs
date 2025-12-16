using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ElliottBot;

public sealed class BotRunner
{
    private readonly ElliottBot _bot;
    private readonly int _warmupCount;
    private readonly int _maxHistory;

    public BotRunner(ElliottBot bot, int warmupCount = 200, int maxHistory = 500)
    {
        _bot = bot;
        _warmupCount = warmupCount;
        _maxHistory = Math.Max(maxHistory, warmupCount);
    }

    public async Task RunAsync(ICandleFeed feed, CancellationToken ct)
    {
        Console.WriteLine($"=== FEED: {feed.Name} ===");

        var history = (await feed.GetWarmupAsync(_warmupCount, ct))
            .OrderBy(c => c.Time)
            .ToList();

        Console.WriteLine($"Warmup candles: {history.Count}");

        await foreach (var candle in feed.StreamAsync(ct))
        {
            if (history.Count > 0 && candle.Time <= history[^1].Time)
            {
                Console.WriteLine($"[SKIP DUP/OLD] {candle.Time:yyyy-MM-dd HH:mm}");
                continue;
            }

            Console.WriteLine($"[CANDLE] {candle.Time:yyyy-MM-dd HH:mm} O={candle.Open} H={candle.High} L={candle.Low} C={candle.Close}");

            // ВАЖЛИВО: history тут НЕ містить candle
            _bot.OnNewCandle(history, candle);

            // тепер додаємо candle в історію для наступних ітерацій
            history.Add(candle);

            if (history.Count > _maxHistory)
                history.RemoveRange(0, history.Count - _maxHistory);
        }
    }
}

