using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication; // якщо будуть ключі, поки можна не використовувати

namespace ElliottBot;

public interface IMarketDataSource
{
    Task<List<Candle>> GetHistoricalCandlesAsync(
        string symbol,
        KlineInterval interval,
        DateTime start,
        DateTime end
    );
}

public class BinanceDataSource : IMarketDataSource
{
    private readonly BinanceRestClient _client;

    public BinanceDataSource()
    {
        _client = new BinanceRestClient();
        // Якщо потрібні ключі – можна буде додати:
        // _client = new BinanceClient(new BinanceClientOptions
        // {
        //     ApiCredentials = new ApiCredentials("KEY", "SECRET")
        // });
    }

    public async Task<List<Candle>> GetHistoricalCandlesAsync(
        string symbol,
        KlineInterval interval,
        DateTime start,
        DateTime end
    )
    {
        var result = await _client.SpotApi.ExchangeData.GetKlinesAsync(
            symbol,
            interval,
            startTime: start,
            endTime: end
        );

        if (!result.Success)
            throw new Exception($"Binance klines error: {result.Error}");

        var list = new List<Candle>();

        foreach (var k in result.Data)
        {
            list.Add(new Candle(
                Time: k.OpenTime,
                Open: k.OpenPrice,
                High: k.HighPrice,
                Low: k.LowPrice,
                Close: k.ClosePrice,
                Volume: k.Volume
            ));
        }

        return list;
    }
}
