using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElliottBot;

namespace ElliottBot
{
    // Базові моделі

    public enum Side
    {
        Flat,
        Long,
        Short
    }


    public enum PivotType
    {
        High,
        Low
    }

    public enum CorrectionDirection
    {
        Down,   // корекція вниз після ап-тренду
        Up      // корекція вгору після даун-тренду
    }
    public readonly record struct CorrectionCandidate(
    int StartSwingIndex,
    int EndSwingIndex,
    CorrectionDirection Direction,
    decimal Score,

    // Додатково: які саме swings утворили A-B-C
    Swing SwingA,
    Swing SwingB,
    Swing SwingC
);

    //public record Candle(
    public readonly record struct Candle(
        DateTime Time,
        decimal Open,
        decimal High,
        decimal Low,
        decimal Close,
        decimal Volume
    );

    public record ElliottSignal(
        Side Side,
        decimal EntryPrice,
        decimal StopLoss,
        decimal TakeProfit,
        string Comment
    );

    public readonly record struct Position(
        Side Side,
        decimal Size,          // к-сть монет (BTC, TON і т.д.)
        decimal EntryPrice,
        decimal StopLoss,
        decimal TakeProfit,
        DateTime EntryTime
    );

    public readonly record struct Pivot(
    int Index,        // індекс свічки в масиві
    decimal Price,    // ціна піку/дна
    PivotType Type    // High або Low
    );
    public enum SwingDirection
    {
        Up,
        Down
    }

    public readonly record struct Swing(
        Pivot From,
        Pivot To,
        SwingDirection Direction,
        decimal Length    // різниця в ціні між From і To (по модулю)
    );
    public enum ImpulseDirection
    {
        Up,
        Down
    }

    public readonly record struct ImpulseCandidate(
        int StartSwingIndex,       // індекс першого swing у списку
        int EndSwingIndex,         // індекс останнього swing (Start + 4)
        ImpulseDirection Direction,
        decimal Score              // "наскільки схоже на імпульс" (поки примітивно)
    );

    public readonly record struct Trade(
    DateTime EntryTime,
    DateTime ExitTime,
    Side Side,
    decimal Entry,
    decimal Exit,
    decimal Size,
    decimal Pnl
);

    public readonly record struct DetectedSetup(
    Side Side,
    ImpulseCandidate Impulse,
    string Comment
);

    public class BotConfig
    {
        public decimal StartingBalance { get; init; } = 1000m;
        public decimal RiskPerTrade { get; init; } = 0.01m;   // 1%
        public string Symbol { get; init; } = "BTCUSDT";
    }

}
