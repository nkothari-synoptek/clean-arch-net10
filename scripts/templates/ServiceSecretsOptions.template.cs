namespace {{ServiceName}}.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed secrets consumed by the service.
/// </summary>
public sealed class ServiceSecretsOptions
{
    public const string SectionName = "ServiceSecrets";

    public DatabaseSecretsOptions Database { get; set; } = new();

    public CacheSecretsOptions Cache { get; set; } = new();

    public MessagingSecretsOptions Messaging { get; set; } = new();

    public sealed class DatabaseSecretsOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public sealed class CacheSecretsOptions
    {
        public string RedisConnectionString { get; set; } = string.Empty;
    }

    public sealed class MessagingSecretsOptions
    {
        public string? ServiceBusConnectionString { get; set; }

        public string? ServiceBusFullyQualifiedNamespace { get; set; }
    }
}
