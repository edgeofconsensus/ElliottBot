using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ElliottBot;

public interface ICandleFeed
{
    string Name { get; }
    Task<IReadOnlyList<Candle>> GetWarmupAsync(int warmupCount, CancellationToken ct);
    IAsyncEnumerable<Candle> StreamAsync(CancellationToken ct);
}
