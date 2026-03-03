using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Common.Shared.Caching;

/// <summary>
/// Redis implementation of distributed cache service.
/// Follows ASP.NET Core best practices for async operations and proper resource management.
/// </summary>
public sealed class RedisCacheService : IDistributedCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Configure JSON options for optimal performance
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {CacheKey}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {CacheKey}", key);
            return JsonSerializer.Deserialize<T>((string)value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value from cache for key: {CacheKey}", key);
            // Return default instead of throwing to prevent cache failures from breaking the application
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        
        if (expiration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Expiration must be greater than zero.", nameof(expiration));
        }

        try
        {
            var db = _redis.GetDatabase();
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            
            await db.StringSetAsync(key, serialized, expiration);
            
            _logger.LogDebug("Cached value for key: {CacheKey} with expiration: {Expiration}", 
                key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {CacheKey}", key);
            // Don't throw - cache failures should not break the application
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
            
            _logger.LogDebug("Removed cache key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {CacheKey}", key);
            // Don't throw - cache failures should not break the application
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {CacheKey}", key);
            return false;
        }
    }
}
