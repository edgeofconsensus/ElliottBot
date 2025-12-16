using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElliottBot;

namespace ElliottBot;

public class ElliottEngine
{
    private readonly BotConfig _cfg;
    private readonly PivotDetector _pivotDetector;

    public ElliottEngine(BotConfig cfg)
    {
        _cfg = cfg;
        _pivotDetector = new PivotDetector(depth: 3);
    }
    /// <summary>
    /// Рахуємо SL/TP на основі імпульсу:
    /// - для Up: SL трохи нижче мінімального low в імпульсі, TP вище ціни, виходячи з висоти імпульсу;
    /// - для Down: дзеркально.
    /// </summary>
    public (decimal sl, decimal tp)? TryComputeWaveLevels(
    ImpulseCandidate impulse,
    IReadOnlyList<Swing> swings,
    decimal entry
)
    {
        if (impulse.EndSwingIndex >= swings.Count)
            return null;

        var window = swings
            .Skip(impulse.StartSwingIndex)
            .Take(5)
            .ToArray();

        if (window.Length < 5)
            return null;

        var pivots = new List<Pivot>();

        foreach (var s in window)
        {
            pivots.Add(s.From);
            pivots.Add(s.To);
        }

        pivots = pivots
            .Distinct()
            .OrderBy(p => p.Index)
            .ToList();

        if (pivots.Count == 0)
            return null;

        var prices = pivots.Select(p => p.Price).ToArray();
        var minPrice = prices.Min();
        var maxPrice = prices.Max();

        if (minPrice <= 0 || maxPrice <= 0 || maxPrice <= minPrice)
            return null;

        decimal sl;
        decimal tp;

        // множник R для TP (risk:reward)
        const decimal R = 2.0m;

        if (impulse.Direction == ImpulseDirection.Up)
        {
            sl = minPrice * 0.998m;
            if (entry <= sl) return null;

            var risk = entry - sl;
            tp = entry + R * risk;
        }
        else
        {
            sl = maxPrice * 1.002m;
            if (entry >= sl) return null;

            var risk = sl - entry;
            tp = entry - R * risk;
        }


        return (sl, tp);
    }


    public DetectedSetup? DetectSetup(IReadOnlyList<Candle> candles)
    {
        if (candles.Count < 60)
            return null;

        var rawPivots = _pivotDetector.Detect(candles);
        if (rawPivots.Count < 6)
            return null;

        var filteredPivots = PivotUtils.FilterByMinMove(rawPivots, candles, 0.7m);

        if (filteredPivots.Count < 6)
            return null;

        var swings = PivotUtils.BuildSwings(filteredPivots);
        if (swings.Count < 5)
            return null;

        var impulses = ImpulseScanner.FindImpulseCandidates(swings);
        if (impulses.Count == 0)
            return null;

        var lastSwingIndex = swings.Count - 1;

        var relevantImpulses = impulses
            .Where(i => i.EndSwingIndex >= lastSwingIndex - 2)
            .OrderByDescending(i => i.Score)
            .ToList();

        if (relevantImpulses.Count == 0)
            return null;

        var best = relevantImpulses[0];

        var side = best.Direction == ImpulseDirection.Up ? Side.Long : Side.Short;
        const decimal minImpulseScore = 0.70m;
       
        //if (best.Score < minImpulseScore)
        //    return null;

        return new DetectedSetup(
        side,
        best,
        $"impulse [{best.StartSwingIndex}..{best.EndSwingIndex}] score={best.Score:F2}"
    );
    }
}
