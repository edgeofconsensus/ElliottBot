using System;
using System.Collections.Generic;
using System.Linq;

namespace ElliottBot
{
public static class CorrectionScanner
{

    public static List<CorrectionCandidate> FindAbcCandidates(IReadOnlyList<Swing> swings)
    {
        var result = new List<CorrectionCandidate>();

        if (swings.Count < 3)
            return result;

        for (int i = 0; i <= swings.Count - 3; i++)
        {
            var a = swings[i];
            var b = swings[i + 1];
            var c = swings[i + 2];

            // Варіант 1: A–B–C вниз (Down, Up, Down)
            if (a.Direction == SwingDirection.Down &&
                b.Direction == SwingDirection.Up &&
                c.Direction == SwingDirection.Down)
            {
                var score = ScoreAbc(a, b, c);
                if (score > 0)
                {
                    result.Add(new CorrectionCandidate(
                        StartSwingIndex: i,
                        EndSwingIndex: i + 2,
                        Direction: CorrectionDirection.Down,
                        Score: score,
                        SwingA: a,
                        SwingB: b,
                        SwingC: c
                    ));
                }
            }

            // Варіант 2: A–B–C вгору (Up, Down, Up)
            if (a.Direction == SwingDirection.Up &&
                b.Direction == SwingDirection.Down &&
                c.Direction == SwingDirection.Up)
            {
                var score = ScoreAbc(a, b, c);
                if (score > 0)
                {
                    result.Add(new CorrectionCandidate(
                        StartSwingIndex: i,
                        EndSwingIndex: i + 2,
                        Direction: CorrectionDirection.Up,
                        Score: score,

                        SwingA: a,
                        SwingB: b,
                        SwingC: c
                    ));
                }
            }
        }

        return result;
    }

    private static decimal ScoreAbc(Swing a, Swing b, Swing c)
    {
        var lenA = a.Length;
        var lenB = b.Length;
        var lenC = c.Length;

        if (lenA <= 0 || lenB <= 0 || lenC <= 0)
            return 0m;

        decimal score = 0m;

        // B менша за A
        if (lenB < lenA)
            score += 0.4m;

        // C близька до A
        var ratioC = lenC / lenA;
        if (ratioC is > 0.7m and < 1.3m)
            score += 0.4m;

        // трохи бонусу, якщо B значно коротша
        if (lenB < lenA * 0.7m)
            score += 0.2m;

        // якщо нічого з цього не виконалось – не беремо
        return score >= 0.5m ? score : 0m;
    }
}
}