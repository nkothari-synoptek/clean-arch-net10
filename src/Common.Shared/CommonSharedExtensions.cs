using Common.Shared.Authentication;
using Common.Shared.Caching;
using Common.Shared.Logging;
using Common.Shared.Observability;
using Common.Shared.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Shared;

/// <summary>
/// Extension methods for registering all Common.Shared services.
/// Provides a unified entry point for infrastructure configuration.
/// </summary>
public static class CommonSharedExtensions
{
    /// <summary>
    /// Adds all Common.Shared infrastructure services to the service collection.
    /// This includes Redis caching, Service Bus messaging, OpenTelemetry observability,
    /// and service-to-service authentication.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="serviceName">Optional service name for observability.</param>
    /// <param name="serviceVersion">Optional service version for observability.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommonSharedServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string? serviceName = null,
        string? serviceVersion = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Add Redis cache if configured
        var redisConnectionString =
            configuration.GetConnectionString("Redis") ??
            configuration["ServiceSecrets:Cache:RedisConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddRedisCache(configuration);
        }

        // Add Service Bus if configured
        var serviceBusConnectionString =
            configuration.GetConnectionString("ServiceBus") ??
            configuration["ServiceSecrets:Messaging:ServiceBusConnectionString"];
        var serviceBusNamespace =
            configuration["ServiceBus:FullyQualifiedNamespace"] ??
            configuration["ServiceSecrets:Messaging:ServiceBusFullyQualifiedNamespace"];
        if (!string.IsNullOrWhiteSpace(serviceBusConnectionString) || 
            !string.IsNullOrWhiteSpace(serviceBusNamespace))
        {
            services.AddServiceBus(configuration);
        }

        // Add OpenTelemetry observability
        services.AddObservability(configuration, serviceName, serviceVersion);

        // Add common logging
        services.AddCommonLogging(configuration);

        // Add service-to-service authentication if configured
        var azureAdSection = configuration.GetSection("AzureAd");
        if (azureAdSection.Exists())
        {
            services.AddServiceToServiceAuthentication(configuration);
        }

        return services;
    }

    /// <summary>
    /// Adds Common.Shared services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="options">Configuration options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommonSharedServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CommonSharedOptions> options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(options);

        var sharedOptions = new CommonSharedOptions();
        options(sharedOptions);

        // Add services based on options
        if (sharedOptions.EnableRedisCache)
        {
            services.AddRedisCache(configuration);
        }

        if (sharedOptions.EnableServiceBus)
        {
            services.AddServiceBus(configuration);
        }

        if (sharedOptions.EnableObservability)
        {
            services.AddObservability(
                configuration,
                sharedOptions.ServiceName,
                sharedOptions.ServiceVersion);
        }

        if (sharedOptions.EnableLogging)
        {
            services.AddCommonLogging(configuration);
        }

        if (sharedOptions.EnableAuthentication)
        {
            services.AddServiceToServiceAuthentication(configuration);
        }

        return services;
    }
}

/// <summary>
/// Configuration options for Common.Shared services.
/// </summary>
public sealed class CommonSharedOptions
{
    /// <summary>
    /// Gets or sets whether to enable Redis caching. Default is true.
    /// </summary>
    public bool EnableRedisCache { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Service Bus messaging. Default is true.
    /// </summary>
    public bool EnableServiceBus { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable OpenTelemetry observability. Default is true.
    /// </summary>
    public bool EnableObservability { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable common logging. Default is true.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable service-to-service authentication. Default is true.
    /// </summary>
    public bool EnableAuthentication { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name for observability.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service version for observability.
    /// </summary>
    public string? ServiceVersion { get; set; }
}
