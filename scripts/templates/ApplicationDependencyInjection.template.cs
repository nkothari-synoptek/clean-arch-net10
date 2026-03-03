using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using {{ServiceName}}.Application.Common.Behaviors;

namespace {{ServiceName}}.Application;

/// <summary>
/// Extension methods for registering Application layer services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Application layer services including MediatR and FluentValidation
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Register MediatR behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
