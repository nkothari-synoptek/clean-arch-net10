using InspectionService.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;

namespace InspectionService.Api.HealthChecks;

public sealed class PostgresSecretHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<ServiceSecretsOptions> _serviceSecretsMonitor;

    public PostgresSecretHealthCheck(IOptionsMonitor<ServiceSecretsOptions> serviceSecretsMonitor)
    {
        _serviceSecretsMonitor = serviceSecretsMonitor;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _serviceSecretsMonitor.CurrentValue.Database.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("Database connection string is missing.");
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await connection.CloseAsync();

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL health check failed.", exception);
        }
    }
}
