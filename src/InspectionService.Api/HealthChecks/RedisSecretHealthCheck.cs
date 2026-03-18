using InspectionService.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace InspectionService.Api.HealthChecks;

public sealed class RedisSecretHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<ServiceSecretsOptions> _serviceSecretsMonitor;

    public RedisSecretHealthCheck(IOptionsMonitor<ServiceSecretsOptions> serviceSecretsMonitor)
    {
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
            await using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
            var database = connection.GetDatabase();
            await database.PingAsync();

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis health check failed.", exception);
        }
    }
}
