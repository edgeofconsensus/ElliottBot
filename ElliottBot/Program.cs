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

        // 1) BACKTEST feed (ти його зробиш як окремий клас, або тимчасово прямо тут)
        // 2) LIVE paper feed:
        var liveFeed = new BinanceLiveCandleFeed(ds, "BTCUSDT", KlineInterval.OneMinute);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        await runner.RunAsync(liveFeed, cts.Token);

        var stats = bot.GetStats();
        Console.WriteLine("=== STATS ===");
        Console.WriteLine($"Balance: {stats.Balance}");
        Console.WriteLine($"Max DD: {stats.MaxDrawdown:P2}");
        Console.WriteLine($"Closed trades: {stats.ClosedTrades} (wins: {stats.WinTrades})");
        Console.WriteLine($"Pending created: {stats.PendingCreated}");
        Console.WriteLine($"Pending filled: {stats.PendingFilled}");
        Console.WriteLine($"Pending canceled: {stats.PendingCanceled}");

        Console.WriteLine("Stopped.");
    }
}
