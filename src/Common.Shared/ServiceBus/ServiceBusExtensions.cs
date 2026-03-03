using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Shared.ServiceBus;

/// <summary>
/// Extension methods for registering Azure Service Bus services.
/// </summary>
public static class ServiceBusExtensions
{
    /// <summary>
    /// Adds Azure Service Bus messaging services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServiceBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("ServiceBus");
        var fullyQualifiedNamespace = configuration["ServiceBus:FullyQualifiedNamespace"];

        // Register ServiceBusClient as singleton (recommended by Azure SDK)
        services.AddSingleton(sp =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                // Use connection string for development
                var clientOptions = new ServiceBusClientOptions
                {
                    TransportType = ServiceBusTransportType.AmqpWebSockets,
                    RetryOptions = new ServiceBusRetryOptions
                    {
                        Mode = ServiceBusRetryMode.Exponential,
                        MaxRetries = 3,
                        Delay = TimeSpan.FromSeconds(1),
                        MaxDelay = TimeSpan.FromSeconds(30)
                    }
                };

                return new ServiceBusClient(connectionString, clientOptions);
            }
            else if (!string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
            {
                // Use managed identity for production
                var clientOptions = new ServiceBusClientOptions
                {
                    TransportType = ServiceBusTransportType.AmqpWebSockets,
                    RetryOptions = new ServiceBusRetryOptions
                    {
                        Mode = ServiceBusRetryMode.Exponential,
                        MaxRetries = 3,
                        Delay = TimeSpan.FromSeconds(1),
                        MaxDelay = TimeSpan.FromSeconds(30)
                    }
                };

                return new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential(), clientOptions);
            }
            else
            {
                throw new InvalidOperationException(
                    "Service Bus connection string or fully qualified namespace is not configured.");
            }
        });

        // Register publisher and consumer as singletons (stateless, thread-safe)
        services.AddSingleton<IMessagePublisher, ServiceBusPublisher>();
        services.AddSingleton<IMessageConsumer, ServiceBusConsumer>();

        return services;
    }
}
