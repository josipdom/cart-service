using System.Text.Json;
using Cart.Application.Abstractions;
using Cart.Application.Carts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Caching;

// The cache is only an optimization: if Redis is unavailable, the failure is logged
// and the request falls back to the database instead of failing.
internal sealed class RedisCartCache(IDistributedCache cache, ILogger<RedisCartCache> logger) : ICartCache
{
    // Short TTL limits how long a stale cart can survive if a read races with a write.
    private static readonly DistributedCacheEntryOptions EntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CartDto?> GetAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var json = await cache.GetStringAsync(Key(userId), cancellationToken);
            return json is null ? null : JsonSerializer.Deserialize<CartDto>(json, JsonOptions);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Failed to read cart for user {UserId} from cache", userId);
            return null;
        }
    }

    public async Task SetAsync(string userId, CartDto cart, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(cart, JsonOptions);
            await cache.SetStringAsync(Key(userId), json, EntryOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Failed to write cart for user {UserId} to cache", userId);
        }
    }

    public async Task RemoveAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            await cache.RemoveAsync(Key(userId), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Failed to remove cart for user {UserId} from cache", userId);
        }
    }

    private static string Key(string userId) => $"cart:{userId}";
}
