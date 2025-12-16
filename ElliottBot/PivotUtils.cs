using System;
using System.Collections.Generic;

namespace ElliottBot;

public static class PivotUtils
{
    /// <summary>
    /// Фільтруємо піводи за мінімальним рухом в ціні між сусідніми піводами.
    /// minMovePercent задається у %, наприклад 0.5m = 0.5%.
    /// </summary>
    public static List<Pivot> FilterByMinMove(
        IReadOnlyList<Pivot> pivots,
        IReadOnlyList<Candle> candles,
        decimal minMovePercent
    )
    {
        if (pivots.Count < 2)
            return new List<Pivot>(pivots);

        var result = new List<Pivot> { pivots[0] };

        for (int i = 1; i < pivots.Count; i++)
        {
            var prev = result[^1];
            var current = pivots[i];

            var prevPrice = prev.Price;
            var currPrice = current.Price;

            if (prevPrice <= 0)
                continue;

            var movePercent = Math.Abs((currPrice - prevPrice) / prevPrice) * 100m;

            if (movePercent >= minMovePercent)
            {
                result.Add(current);
            }
            else
            {
                // дрібний рух – ігноруємо як шум
            }
        }

        return result;
    }

    /// <summary>
    /// Будуємо "ноги" (swings) між послідовними піводами.
    /// </summary>
    public static List<Swing> BuildSwings(IReadOnlyList<Pivot> pivots)
    {
        var swings = new List<Swing>();

        if (pivots.Count < 2)
            return swings;

        for (int i = 1; i < pivots.Count; i++)
        {
            var from = pivots[i - 1];
            var to = pivots[i];

            var direction = to.Price > from.Price
                ? SwingDirection.Up
                : SwingDirection.Down;

            var length = Math.Abs(to.Price - from.Price);

            swings.Add(new Swing(
                From: from,
                To: to,
                Direction: direction,
                Length: length
            ));
        }

        return swings;
    }
}
