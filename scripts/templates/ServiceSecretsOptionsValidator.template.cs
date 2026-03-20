using Microsoft.Extensions.Options;

namespace {{ServiceName}}.Infrastructure.Configuration;

/// <summary>
/// Validates required secret configuration in one place.
/// </summary>
public sealed class ServiceSecretsOptionsValidator : IValidateOptions<ServiceSecretsOptions>
{
    public ValidateOptionsResult Validate(string? name, ServiceSecretsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.Database is null)
        {
            failures.Add($"{ServiceSecretsOptions.SectionName}:Database section is required.");
        }
        else if (string.IsNullOrWhiteSpace(options.Database.ConnectionString))
        {
            failures.Add($"{ServiceSecretsOptions.SectionName}:Database:ConnectionString is required.");
        }

        if (options.Cache is null)
        {
            failures.Add($"{ServiceSecretsOptions.SectionName}:Cache section is required.");
        }
        else if (string.IsNullOrWhiteSpace(options.Cache.RedisConnectionString))
        {
            failures.Add($"{ServiceSecretsOptions.SectionName}:Cache:RedisConnectionString is required.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
