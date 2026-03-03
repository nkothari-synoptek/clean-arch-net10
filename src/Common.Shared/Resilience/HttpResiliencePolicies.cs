using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Common.Shared.Resilience;

/// <summary>
/// Provides HTTP resilience policies using Polly.
/// Implements retry, circuit breaker, and timeout patterns.
/// </summary>
public static class HttpResiliencePolicies
{
    /// <summary>
    /// Gets a retry policy with exponential backoff for transient HTTP errors.
    /// </summary>
    /// <returns>An async retry policy.</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    if (logger != null)
                    {
                        var reason = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString();
                        logger.LogWarning(
                            "HTTP request retry {RetryCount} after {DelayMs}ms due to {Reason}",
                            retryCount,
                            timespan.TotalMilliseconds,
                            reason);
                    }
                });
    }

    /// <summary>
    /// Gets a circuit breaker policy to prevent cascading failures.
    /// </summary>
    /// <returns>An async circuit breaker policy.</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration, context) =>
                {
                    var logger = context.GetLogger();
                    if (logger != null)
                    {
                        var reason = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString();
                        logger.LogWarning(
                            "Circuit breaker opened for {DurationSeconds}s due to {Reason}",
                            duration.TotalSeconds,
                            reason);
                    }
                },
                onReset: context =>
                {
                    var logger = context.GetLogger();
                    logger?.LogInformation("Circuit breaker reset");
                },
                onHalfOpen: () =>
                {
                    // Circuit breaker is testing if the service has recovered
                });
    }

    /// <summary>
    /// Gets a timeout policy to prevent long-running requests.
    /// </summary>
    /// <returns>An async timeout policy.</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: TimeSpan.FromSeconds(10),
            onTimeoutAsync: (context, timespan, task) =>
            {
                var logger = context.GetLogger();
                logger?.LogWarning(
                    "HTTP request timed out after {TimeoutSeconds}s",
                    timespan.TotalSeconds);
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Helper method to get logger from Polly context.
    /// </summary>
    private static ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue("Logger", out var logger))
        {
            return logger as ILogger;
        }
        return null;
    }
}
