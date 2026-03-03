using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Common.Shared.Caching;

/// <summary>
/// Extension methods for registering Redis cache services.
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// Adds Redis distributed cache service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is not configured.");

        // Register IConnectionMultiplexer as singleton (recommended by StackExchange.Redis)
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configOptions = ConfigurationOptions.Parse(connectionString);
            
            // Best practices for production
            configOptions.AbortOnConnectFail = false;
            configOptions.ConnectRetry = 3;
            configOptions.ConnectTimeout = 5000;
            configOptions.SyncTimeout = 5000;
            configOptions.AsyncTimeout = 5000;
            
            return ConnectionMultiplexer.Connect(configOptions);
        });

        // Register cache service as singleton (stateless, thread-safe)
        services.AddSingleton<IDistributedCacheService, RedisCacheService>();

        return services;
    }
}
