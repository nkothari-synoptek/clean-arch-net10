using Common.Shared;
using InspectionService.Application.Inspections.Interfaces;
using InspectionService.Infrastructure.Messaging;
using InspectionService.Infrastructure.Persistence;
using InspectionService.Infrastructure.Persistence.Repositories.Inspections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InspectionService.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure layer services including DbContext, repositories, and external services
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Common.Shared services (Redis cache, Service Bus, OpenTelemetry, etc.)
        services.AddCommonSharedServices(configuration);

        // Register DbContext with PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });
        });

        // Register repositories
        services.AddScoped<IInspectionRepository, InspectionRepository>();

        // Register event publishers
        services.AddScoped<InspectionEventPublisher>();

        return services;
    }

    /// <summary>
    /// Configures HTTP clients with resilience policies for external service integrations
    /// </summary>
    public static IServiceCollection AddExternalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Example: Configure HTTP client for external notification service
        // services.AddHttpClient<INotificationService, NotificationServiceAdapter>(client =>
        // {
        //     client.BaseAddress = new Uri(configuration["Services:NotificationService:BaseUrl"]);
        // })
        // .AddCommonResiliencePolicies()   // From Common.Shared
        // .AddServiceAuthentication();      // From Common.Shared

        // Example: Configure HTTP client for master data service
        // services.AddHttpClient<IMasterDataService, MasterDataServiceAdapter>(client =>
        // {
        //     client.BaseAddress = new Uri(configuration["Services:MasterDataService:BaseUrl"]);
        // })
        // .AddCommonResiliencePolicies()   // From Common.Shared
        // .AddServiceAuthentication();      // From Common.Shared

        return services;
    }
}
