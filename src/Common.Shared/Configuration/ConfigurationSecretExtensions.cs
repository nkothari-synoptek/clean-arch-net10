using Microsoft.Extensions.Configuration;

namespace Common.Shared.Configuration;

/// <summary>
/// Centralized configuration lookups for shared infrastructure secrets.
/// </summary>
public static class ConfigurationSecretExtensions
{
    public static string? GetRedisConnectionString(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.GetConnectionString("Redis")
            ?? configuration["ServiceSecrets:Cache:RedisConnectionString"];
    }

    public static string? GetServiceBusConnectionString(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.GetConnectionString("ServiceBus")
            ?? configuration["ServiceSecrets:Messaging:ServiceBusConnectionString"];
    }

    public static string? GetServiceBusFullyQualifiedNamespace(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration["ServiceBus:FullyQualifiedNamespace"]
            ?? configuration["ServiceSecrets:Messaging:ServiceBusFullyQualifiedNamespace"];
    }
}
