using System;
using System.Collections.Generic;

namespace ElliottBot;

public static class CandleFactory
{    
    public static List<Candle> GenerateTrend(
        int count,
        decimal startPrice,
        decimal step,
        bool upTrend,
        TimeSpan? frame = null
    )
    {

        var candles = new List<Candle>(count);
        var ts = frame ?? TimeSpan.FromHours(1);

        var time = DateTime.UtcNow - ts * count;
        var price = startPrice;

        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            // Тренд:
            price += (upTrend ? step : -step);

            // Трошки шуму, щоб не були ідеально рівні
            var noise = (decimal)(random.NextDouble() * (double)step * 0.2) - step * 0.1m;

            var close = price + noise;
            var open = close - step * (upTrend ? 0.3m : -0.3m);

            var high = Math.Max(open, close) + step * 0.1m;
            var low = Math.Min(open, close) - step * 0.1m;

            var volume = (decimal)(100 + random.Next(0, 50));

            time += ts;

            candles.Add(new Candle(
                time,
                open,
                high,
                low,
                close,
                volume
            ));
        }

        return candles;
    }
    public static List<Candle> GenerateWavePattern(
    int count = 100,
    decimal centerPrice = 30000m,
    decimal amplitude = 500m,
    decimal noiseLevel = 50m
)
    {
        var candles = new List<Candle>(count);
        var ts = TimeSpan.FromHours(1);
        var time = DateTime.UtcNow - ts * count;

        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            time += ts;

            // синусоїда
            var angle = i * 0.3; // частота коливань
            var wave = (decimal)Math.Sin(angle);

            var basePrice = centerPrice + wave * amplitude;

            // невеликий шум
            var noise = (decimal)(random.NextDouble() * (double)noiseLevel * 2) - noiseLevel;

            var close = basePrice + noise;
            var open = close + (decimal)(random.NextDouble() * 40 - 20);

            var high = Math.Max(open, close) + 20;
            var low = Math.Min(open, close) - 20;

            var volume = (decimal)(100 + random.Next(0, 50));

            candles.Add(new Candle(
                time,
                open,
                high,
                low,
                close,
                volume
            ));
        }

        return candles;
    }


    public static List<Candle> GenerateUptrend(
        int count = 60,
        decimal startPrice = 30000m,
        decimal step = 100m
    ) => GenerateTrend(count, startPrice, step, upTrend: true);

    public static List<Candle> GenerateDowntrend(
        int count = 60,
        decimal startPrice = 30000m,
        decimal step = 100m
    ) => GenerateTrend(count, startPrice, step, upTrend: false);
}