using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElliottBot;

namespace ElliottBot
{

    public class ElliottBot
    {
        private readonly BotConfig _cfg;
        private readonly ElliottEngine _engine;
        private readonly RiskManager _risk;

        private Position? _openPosition;

        private int _closedTrades;
        private int _winTrades;

        private DateTime? _lastExitTime;

        public decimal Balance => _risk.Balance;
        public int ClosedTrades => _closedTrades;
        public int WinTrades => _winTrades;

        private readonly List<Trade> _trades = new();
        public IReadOnlyList<Trade> Trades => _trades;

        private decimal _peakBalance;
        private decimal _maxDrawdown;

        public decimal MaxDrawdown => _maxDrawdown;

        private int _setupsSeen;
        private int _abcInZone;
        private int _abcBlocked;
        public int SetupsSeen => _setupsSeen;
        public int AbcInZone => _abcInZone;
        public int AbcBlocked => _abcBlocked;

        public ElliottBot(BotConfig cfg)
        {
            _cfg = cfg;
            _engine = new ElliottEngine(cfg);
            _risk = new RiskManager(cfg);
            _peakBalance = _risk.Balance;
        }

        private void AfterBalanceUpdate()
        {
            if (_risk.Balance > _peakBalance) _peakBalance = _risk.Balance;
            var dd = (_peakBalance - _risk.Balance) / _peakBalance; // 0.0..1.0
            if (dd > _maxDrawdown) _maxDrawdown = dd;
        }

        public decimal CalcPositionSize(ElliottSignal signal, decimal entryPrice)
        {
            var riskAmount = Balance * _cfg.RiskPerTrade;

            var riskPerUnit = Math.Abs(entryPrice - signal.StopLoss);
            if (riskPerUnit <= 0) return 0m;

            return riskAmount / riskPerUnit;
        }

        public void OnNewCandle(IReadOnlyList<Candle> history, Candle current)
        {
            // 1) спочатку супроводжуємо відкриту позицію на поточній свічці
            if (_openPosition is not null)
                CheckPosition(current);

            if (_lastExitTime is not null)
            {
                // 3 години паузи після закриття
                if (current.Time <= _lastExitTime.Value.AddHours(3))
                    return;
            }


            // 2) якщо позиції нема — шукаємо сигнал ТІЛЬКИ на history
            if (_openPosition is not null)
                return;

            var entry = current.Open;

            var setup = _engine.DetectSetup(history);
            if (setup is null)
                return;
            _setupsSeen++;


            // треба swings для TryComputeWaveLevels
            var rawPivots = new PivotDetector(depth: 3).Detect(history);
            var filteredPivots = PivotUtils.FilterByMinMove(rawPivots, history, 0.7m);
            var swings = PivotUtils.BuildSwings(filteredPivots);
            var abcCandidates = CorrectionScanner.FindAbcCandidates(swings);
            
            if (abcCandidates.Count > 0)
            {
                const decimal abcMinScore = 0.8m;
                
                var lastSwingIndex = swings.Count - 1;

                var lastAbc = abcCandidates
    .Where(x => x.EndSwingIndex >= lastSwingIndex - 5)
    .OrderByDescending(x => x.EndSwingIndex)
    .FirstOrDefault();

                // FirstOrDefault() для record struct дає дефолтний елемент, тому робимо захист:
                var hasLastAbc =
                    abcCandidates.Any(x => x.EndSwingIndex >= lastSwingIndex - 5);

                if (hasLastAbc && lastAbc.Score >= abcMinScore)
                {
                    // Дістаємо A,B,C як swings[i], swings[i+1], swings[i+2]
                    var i = lastAbc.StartSwingIndex;
                    if (i + 2 < swings.Count)
                    {
                        var swingA = swings[i];
                        var swingB = swings[i + 1];
                        var swingC = swings[i + 2];

                        var abcPrices = new[]
                        {
            swingA.From.Price, swingA.To.Price,
            swingB.From.Price, swingB.To.Price,
            swingC.From.Price, swingC.To.Price
        };


                        var abcMin = abcPrices.Min();
                        var abcMax = abcPrices.Max();

                        var padding = (abcMax - abcMin) * 0.10m; // 10%
                        var zoneMin = abcMin - padding;
                        var zoneMax = abcMax + padding;

                        var inAbcZone = entry >= zoneMin && entry <= zoneMax;

                        if (inAbcZone)
                        {
                            _abcInZone++;

                            // блокуємо тільки коли entry реально “в зоні корекції”
                            if (setup.Value.Side == Side.Long && lastAbc.Direction == CorrectionDirection.Down)
                                return;

                            if (setup.Value.Side == Side.Short && lastAbc.Direction == CorrectionDirection.Up)
                                return;
                        }
                    }

                }
                bool blockedByAbc = false;

                
                var lastAbcCandidates = abcCandidates
                    .Where(x => x.EndSwingIndex >= lastSwingIndex - 5)
                    .OrderByDescending(x => x.EndSwingIndex)
                    .ToList();

                if (lastAbcCandidates.Count > 0)
                {
              
                    if (lastAbc.Score >= abcMinScore)
                    {
                        var i = lastAbc.StartSwingIndex;
                        if (i + 2 < swings.Count)
                        {
                            var a = swings[i];
                            var b = swings[i + 1];
                            var c = swings[i + 2];

                            var abcPrices = new[]
                            {
                a.From.Price, a.To.Price,
                b.From.Price, b.To.Price,
                c.From.Price, c.To.Price
            };

                            var abcMin = abcPrices.Min();
                            var abcMax = abcPrices.Max();
                            var padding = (abcMax - abcMin) * 0.10m;

                            var zoneMin = abcMin - padding;
                            var zoneMax = abcMax + padding;

                            var inAbcZone = entry >= zoneMin && entry <= zoneMax;

                            if (inAbcZone)
                            {
                                if (setup.Value.Side == Side.Long && lastAbc.Direction == CorrectionDirection.Down)
                                    blockedByAbc = true;

                                if (setup.Value.Side == Side.Short && lastAbc.Direction == CorrectionDirection.Up)
                                    blockedByAbc = true;

                                if (blockedByAbc)
                                {
                                    _abcBlocked++;
                                    Console.WriteLine($"BLOCK by ABC | time={current.Time:yyyy-MM-dd HH:mm} side={setup.Value.Side} entry={entry:F2} score={lastAbc.Score:F2}");
                                    return;
                                }
                            }
                        }
                    }
                }
                var levels = _engine.TryComputeWaveLevels(setup.Value.Impulse, swings, entry);
                if (levels is null)
                    return;

                var (sl, tp) = levels.Value;
                var riskPerUnit = Math.Abs(entry - sl);
                var minRisk = entry * 0.0015m;   // 0.15%
                var maxRisk = entry * 0.015m;    // 1.5%
              
                if (riskPerUnit < minRisk || riskPerUnit > maxRisk)
                    return;

                // валідація рівнів
                if (setup.Value.Side == Side.Long)
                {
                    if (!(sl < entry && tp > entry))
                        return;
                }
                else if (setup.Value.Side == Side.Short)
                {
                    if (!(sl > entry && tp < entry))
                        return;
                }
                else return;

                // тепер size рахуємо від entry
                var size = _risk.CalcPositionSize(new ElliottSignal(setup.Value.Side, entry, sl, tp, setup.Value.Comment), entry);
                if (size <= 0)
                    return;
                Console.WriteLine($"Risk/unit: {riskPerUnit} | Risk$: {_risk.Balance * _cfg.RiskPerTrade:F2}");


                _openPosition = new Position(
                    setup.Value.Side,
                    size,
                    entry,
                    sl,
                    tp,
                    current.Time
                );

                Console.WriteLine("=== NEW SIGNAL ===");
                Console.WriteLine($"Time: {current.Time:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"Side: {setup.Value.Side}");
                Console.WriteLine($"Entry: {entry}");
                Console.WriteLine($"SL: {sl}");
                Console.WriteLine($"TP: {tp}");
                Console.WriteLine($"Size: {size}");
                Console.WriteLine($"Comment: {setup.Value.Comment}\n");
            }
        }
        private void CheckPosition(Candle last)
        {
            if (_openPosition is null) return;

            var pos = _openPosition.Value;
            bool closed = false;
            decimal pnl = 0m;

            if (pos.Side == Side.Long)
            {
                // ДЛЯ LONG:
                var hitSl = last.Low <= pos.StopLoss;
                var hitTp = last.High >= pos.TakeProfit;

                if (hitSl && hitTp)
                {
                    // conservative: вважаємо SL
                    closed = true;
                    var exit = pos.StopLoss;
                    pnl = (exit - pos.EntryPrice) * pos.Size;
                    Console.WriteLine("hitSl && hitTp");
                }
                else if (hitSl)
                {
                    closed = true;
                    var exit = pos.StopLoss;
                    pnl = (exit - pos.EntryPrice) * pos.Size;
                }
                else if (hitTp)
                {
                    closed = true;
                    var exit = pos.TakeProfit;
                    pnl = (exit - pos.EntryPrice) * pos.Size;
                }
            }
            else if (pos.Side == Side.Short)
            {
                // ДЛЯ SHORT:
                var hitSl = last.High >= pos.StopLoss;
                var hitTp = last.Low <= pos.TakeProfit;

                if (hitSl && hitTp)
                {
                    // conservative: SL
                    closed = true;
                    var exit = pos.StopLoss;
                    pnl = (pos.EntryPrice - exit) * pos.Size;
                    Console.WriteLine("hitSl && hitTp");
                }
                else if (hitSl)
                {
                    closed = true;
                    var exit = pos.StopLoss;
                    pnl = (pos.EntryPrice - exit) * pos.Size;
                }
                else if (hitTp)
                {
                    closed = true;
                    var exit = pos.TakeProfit;
                    pnl = (pos.EntryPrice - exit) * pos.Size;
                }
            }

            if (closed)
            {
                var exit = 0m;

                // визнач exit точно:
                if (pos.Side == Side.Long)
                {
                    if (last.Low <= pos.StopLoss) exit = pos.StopLoss;
                    else if (last.High >= pos.TakeProfit) exit = pos.TakeProfit;
                }
                else
                {
                    if (last.High >= pos.StopLoss) exit = pos.StopLoss;
                    else if (last.Low <= pos.TakeProfit) exit = pos.TakeProfit;
                }




                _trades.Add(new Trade(
                    EntryTime: pos.EntryTime,
                    ExitTime: last.Time,
                    Side: pos.Side,
                    Entry: pos.EntryPrice,
                    Exit: exit,
                    Size: pos.Size,
                    Pnl: pnl
                ));


                _openPosition = null;
                _risk.UpdateBalance(pnl);
                AfterBalanceUpdate();

                _closedTrades++;
                if (pnl > 0) _winTrades++;

                Console.WriteLine("=== CLOSING POSITION ===");
                Console.WriteLine($"Close range: low={last.Low}, high={last.High}");
                Console.WriteLine($"PNL: {pnl:F2} $");
                Console.WriteLine($"New balance: {_risk.Balance:F2} $\n");
                _lastExitTime = last.Time;
                Console.WriteLine($"Equity: {_risk.Balance:F2}");

            }

        }

    }

}
