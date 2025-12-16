using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElliottBot;

namespace ElliottBot
{
    public class PivotDetector
    {
        private readonly int _depth;

        /// <summary>
        /// depth = скільки свічок ліворуч і праворуч дивимось.
        /// Наприклад, depth = 3 означає:
        /// свічка i – High, якщо її High вище, ніж High у 3 ліворуч і 3 праворуч.
        /// Аналогічно для Low.
        /// </summary>
        public PivotDetector(int depth = 3)
        {
            if (depth < 1)
                throw new ArgumentOutOfRangeException(nameof(depth), "depth must be >= 1");

            _depth = depth;
        }

        public List<Pivot> Detect(IReadOnlyList<Candle> candles)
        {
            var pivots = new List<Pivot>();

            if (candles.Count < _depth * 2 + 1)
                return pivots; // замало даних

            for (int i = _depth; i < candles.Count - _depth; i++)
            {
                var current = candles[i];

                bool isHigh = true;
                bool isLow = true;

                // дивимось вікно від i - depth до i + depth
                for (int j = i - _depth; j <= i + _depth; j++)
                {
                    if (j == i) continue;

                    var other = candles[j];

                    if (other.High >= current.High)
                        isHigh = false;

                    if (other.Low <= current.Low)
                        isLow = false;

                    if (!isHigh && !isLow)
                        break;
                }

                if (isHigh)
                {
                    pivots.Add(new Pivot(
                        Index: i,
                        Price: current.High,
                        Type: PivotType.High
                    ));
                }

                if (isLow)
                {
                    pivots.Add(new Pivot(
                        Index: i,
                        Price: current.Low,
                        Type: PivotType.Low
                    ));
                }
            }

            return pivots;
        }
    }
}
