using System.ComponentModel.DataAnnotations;

namespace InspectionService.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed secrets consumed by the service.
/// </summary>
public sealed class ServiceSecretsOptions
{
    public const string SectionName = "ServiceSecrets";

    [Required]
    public DatabaseSecretsOptions Database { get; set; } = new();

    [Required]
    public CacheSecretsOptions Cache { get; set; } = new();

    public MessagingSecretsOptions Messaging { get; set; } = new();

    public sealed class DatabaseSecretsOptions
    {
        [Required]
        public string ConnectionString { get; set; } = string.Empty;
    }

    public sealed class CacheSecretsOptions
    {
        [Required]
        public string RedisConnectionString { get; set; } = string.Empty;
    }

    public sealed class MessagingSecretsOptions
    {
        public string? ServiceBusConnectionString { get; set; }

        public string? ServiceBusFullyQualifiedNamespace { get; set; }
    }
}
