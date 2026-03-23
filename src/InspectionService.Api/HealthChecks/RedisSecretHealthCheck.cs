using InspectionService.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace InspectionService.Api.HealthChecks;

public sealed class RedisSecretHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IOptionsMonitor<ServiceSecretsOptions> _serviceSecretsMonitor;

    public RedisSecretHealthCheck(
        IConnectionMultiplexer connectionMultiplexer,
        IOptionsMonitor<ServiceSecretsOptions> serviceSecretsMonitor)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _serviceSecretsMonitor = serviceSecretsMonitor;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _serviceSecretsMonitor.CurrentValue.Cache.RedisConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("Redis connection string is missing.");
        }

        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            await database.PingAsync();

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis health check failed.", exception);
        }
    }
}
