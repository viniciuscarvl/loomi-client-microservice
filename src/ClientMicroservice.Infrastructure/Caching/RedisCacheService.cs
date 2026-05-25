using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ClientMicroservice.Application.Common.Interfaces;

namespace ClientMicroservice.Infrastructure.Caching;

internal sealed class RedisCacheService(IDistributedCache cache) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        var data = await cache.GetAsync(key, ct);
        return data is null ? default : JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(value);
        await cache.SetAsync(key, data,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct)
        => cache.RemoveAsync(key, ct);
}
