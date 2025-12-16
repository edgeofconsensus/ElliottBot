using System;
using System.Collections.Generic;
using System.Linq;

namespace ElliottBot;

public static class ImpulseScanner
{
    /// <summary>
    /// Шукаємо найпростіші кандидати в 5-хвильовий імпульс на основі swings:
    /// Патерни:
    ///   Up:   Up, Down, Up, Down, Up
    ///   Down: Down, Up, Down, Up, Down
    /// Без ще жорстких правил Елліота – тільки базова структура + примітивний score.
    /// </summary>
    public static List<ImpulseCandidate> FindImpulseCandidates(IReadOnlyList<Swing> swings)
    {
        var result = new List<ImpulseCandidate>();

        if (swings.Count < 5)
            return result;

        for (int i = 0; i <= swings.Count - 5; i++)
        {
            var window = swings.Skip(i).Take(5).ToArray();

            // Перший напрямок визначає "напрямок" імпульсу
            var dir = window[0].Direction;

            bool patternUp =
                dir == SwingDirection.Up &&
                window[0].Direction == SwingDirection.Up &&
                window[1].Direction == SwingDirection.Down &&
                window[2].Direction == SwingDirection.Up &&
                window[3].Direction == SwingDirection.Down &&
                window[4].Direction == SwingDirection.Up;

            bool patternDown =
                dir == SwingDirection.Down &&
                window[0].Direction == SwingDirection.Down &&
                window[1].Direction == SwingDirection.Up &&
                window[2].Direction == SwingDirection.Down &&
                window[3].Direction == SwingDirection.Up &&
                window[4].Direction == SwingDirection.Down;

            if (!patternUp && !patternDown)
                continue;

            var impulseDir = patternUp ? ImpulseDirection.Up : ImpulseDirection.Down;

            // Простий "score":
            //  - хвиля 3 має бути довша за 1
            //  - хвиля 5 не найкоротша серед 1,3,5
            var w1 = window[0].Length;
            var w2 = window[1].Length;
            var w3 = window[2].Length;
            var w4 = window[3].Length;
            var w5 = window[4].Length;

            decimal score = 0m;

            if (w3 > w1)
                score += 0.4m;

            // 5 не найкоротша
            var min135 = Math.Min(w1, Math.Min(w3, w5));
            if (w5 > min135)
                score += 0.3m;

            // Корекції (2 і 4) трохи менші за імпульсні хвилі 1 і 3
            if (w2 < w1 && w4 < w3)
                score += 0.3m;

            // Якщо score дуже малий – ігноруємо
            if (score < 0.4m)
                continue;

            result.Add(new ImpulseCandidate(
                StartSwingIndex: i,
                EndSwingIndex: i + 4,
                Direction: impulseDir,
                Score: score
            ));
        }

        return result;
    }
}
