using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Shared.Logging;

/// <summary>
/// Extension methods for configuring structured logging.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds common logging configuration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommonLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.AddDebug();
        });

        return services;
    }

    /// <summary>
    /// Creates a scoped logger context with additional properties.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="properties">The properties to add to the scope.</param>
    /// <returns>A disposable scope.</returns>
    public static IDisposable? BeginScopeWithProperties(
        this ILogger logger,
        params (string Key, object Value)[] properties)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (properties.Length == 0)
        {
            return null;
        }

        var state = new Dictionary<string, object>();
        foreach (var (key, value) in properties)
        {
            state[key] = value;
        }

        return logger.BeginScope(state);
    }

    /// <summary>
    /// Logs an operation with timing information.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <returns>A disposable that logs the operation duration when disposed.</returns>
    public static IDisposable LogOperation(this ILogger logger, string operationName)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        return new OperationLogger(logger, operationName);
    }

    private sealed class OperationLogger : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly System.Diagnostics.Stopwatch _stopwatch;

        public OperationLogger(ILogger logger, string operationName)
        {
            _logger = logger;
            _operationName = operationName;
            _stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            _logger.LogDebug("Starting operation: {OperationName}", _operationName);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _logger.LogInformation(
                "Completed operation: {OperationName} in {ElapsedMs}ms",
                _operationName,
                _stopwatch.ElapsedMilliseconds);
        }
    }
}
