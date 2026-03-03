using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Common.Shared.Observability;

/// <summary>
/// Extension methods for configuring OpenTelemetry observability.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry with tracing and metrics to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="serviceName">The service name for telemetry.</param>
    /// <param name="serviceVersion">Optional service version.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string? serviceName = null,
        string? serviceVersion = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var resolvedServiceName = serviceName 
            ?? configuration["ServiceName"] 
            ?? configuration["OTEL_SERVICE_NAME"]
            ?? "UnknownService";
        
        var resolvedServiceVersion = serviceVersion 
            ?? configuration["ServiceVersion"] 
            ?? "1.0.0";

        // Register ActivitySource and Meter providers
        services.AddSingleton(sp => new ActivitySourceProvider(resolvedServiceName, resolvedServiceVersion));
        services.AddSingleton(sp => new MetricsProvider(resolvedServiceName, resolvedServiceVersion));

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: resolvedServiceName,
                    serviceVersion: resolvedServiceVersion);
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(resolvedServiceName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            // Don't trace health check endpoints
                            var path = httpContext.Request.Path.Value ?? string.Empty;
                            return !path.Contains("/health", StringComparison.OrdinalIgnoreCase);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    });

                // Add Azure Monitor exporter if configured
                var connectionString = configuration["ApplicationInsights:ConnectionString"];
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    tracing.AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = connectionString;
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(resolvedServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                // Add Azure Monitor exporter if configured
                var connectionString = configuration["ApplicationInsights:ConnectionString"];
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    metrics.AddAzureMonitorMetricExporter(options =>
                    {
                        options.ConnectionString = connectionString;
                    });
                }
            });

        return services;
    }

    /// <summary>
    /// Adds custom tracing configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for tracing.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCustomTracing(
        this IServiceCollection services,
        Action<TracerProviderBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOpenTelemetry()
            .WithTracing(configure);

        return services;
    }

    /// <summary>
    /// Adds custom metrics configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for metrics.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCustomMetrics(
        this IServiceCollection services,
        Action<MeterProviderBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOpenTelemetry()
            .WithMetrics(configure);

        return services;
    }
}
