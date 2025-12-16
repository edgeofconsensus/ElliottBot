using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElliottBot;

namespace ElliottBot;

public class RiskManager
{
    private readonly BotConfig _cfg;

    public decimal Balance { get; private set; }

    public RiskManager(BotConfig cfg)
    {
        _cfg = cfg;
        Balance = cfg.StartingBalance;
    }

    public void UpdateBalance(decimal pnl)
    {
        Balance += pnl;
    }

    public decimal CalcPositionSize(ElliottSignal signal, decimal entryPrice)
    {
        var riskAmount = Balance * _cfg.RiskPerTrade;
        var riskPerUnit = Math.Abs(entryPrice - signal.StopLoss);
        if (riskPerUnit <= 0) return 0m;

        return riskAmount / riskPerUnit;
    }
}
