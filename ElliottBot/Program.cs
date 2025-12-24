using Binance.Net.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElliottBot;

class Program
{

    static async Task Main()
    {
        Console.WriteLine("ElliottBot started");

        var cfg = new BotConfig
        {
            StartingBalance = 1000m,
            RiskPerTrade = 0.01m,
            Symbol = "BTCUSDT"
        };


        var bot = new ElliottBot(cfg);
        var runner = new BotRunner(bot, warmupCount: 200);

        var ds = new BinanceDataSource();


        var end = DateTime.UtcNow;
        var start = end.AddDays(-90); // або як раніше
        var candles = await ds.GetHistoricalCandlesAsync("BTCUSDT", KlineInterval.OneHour, start, end);

        // warmup: наприклад 200
        int warmup = 200;
        for (int i = warmup; i < candles.Count; i++)
        {
            var history = candles.Take(i).ToList();   // або краще без ToList: зробити slice
            var current = candles[i];

            bot.OnNewCandle(history, current);

            // опційно: раз на N кроків друк статистики
        }

        Console.WriteLine($"DONE bal={bot.Balance} trades={bot.ClosedTrades} win={bot.WinTrades}");

        // 2) LIVE paper feed:
        var liveFeed = new BinanceLiveCandleFeed(ds, "BTCUSDT", KlineInterval.OneMinute);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        await runner.RunAsync(liveFeed, cts.Token);

        Console.WriteLine("Stopped.");
    }
}
