using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace Common.Shared.Resilience;

/// <summary>
/// Extension methods for adding HTTP resilience policies to HttpClient.
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    /// Adds common resilience policies (retry, circuit breaker, timeout) to an HttpClient.
    /// Policies are applied in the order: timeout -> retry -> circuit breaker.
    /// </summary>
    /// <param name="builder">The HttpClient builder.</param>
    /// <returns>The HttpClient builder for chaining.</returns>
    public static IHttpClientBuilder AddCommonResiliencePolicies(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddPolicyHandler((services, request) =>
            {
                var logger = services.GetRequiredService<ILogger<HttpClient>>();
                var context = new Context { ["Logger"] = logger };
                
                // Wrap policies: timeout -> retry -> circuit breaker
                return Policy.WrapAsync(
                    HttpResiliencePolicies.GetTimeoutPolicy(),
                    HttpResiliencePolicies.GetRetryPolicy(),
                    HttpResiliencePolicies.GetCircuitBreakerPolicy()
                );
            });
    }

    /// <summary>
    /// Adds a custom retry policy to an HttpClient.
    /// </summary>
    /// <param name="builder">The HttpClient builder.</param>
    /// <param name="retryCount">Number of retry attempts.</param>
    /// <param name="baseDelay">Base delay for exponential backoff.</param>
    /// <returns>The HttpClient builder for chaining.</returns>
    public static IHttpClientBuilder AddRetryPolicy(
        this IHttpClientBuilder builder,
        int retryCount = 3,
        TimeSpan? baseDelay = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (retryCount < 0)
        {
            throw new ArgumentException("Retry count must be non-negative.", nameof(retryCount));
        }

        var delay = baseDelay ?? TimeSpan.FromSeconds(1);

        return builder.AddPolicyHandler((services, request) =>
        {
            var logger = services.GetRequiredService<ILogger<HttpClient>>();
            
            return Polly.Extensions.Http.HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * delay.TotalSeconds),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        var reason = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString();
                        logger.LogWarning(
                            "HTTP request retry {RetryCount}/{MaxRetries} after {DelayMs}ms due to {Reason}",
                            retryAttempt,
                            retryCount,
                            timespan.TotalMilliseconds,
                            reason);
                    });
        });
    }

    /// <summary>
    /// Adds a custom circuit breaker policy to an HttpClient.
    /// </summary>
    /// <param name="builder">The HttpClient builder.</param>
    /// <param name="failureThreshold">Number of failures before breaking.</param>
    /// <param name="breakDuration">Duration to keep circuit open.</param>
    /// <returns>The HttpClient builder for chaining.</returns>
    public static IHttpClientBuilder AddCircuitBreakerPolicy(
        this IHttpClientBuilder builder,
        int failureThreshold = 5,
        TimeSpan? breakDuration = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (failureThreshold <= 0)
        {
            throw new ArgumentException("Failure threshold must be positive.", nameof(failureThreshold));
        }

        var duration = breakDuration ?? TimeSpan.FromSeconds(30);

        return builder.AddPolicyHandler((services, request) =>
        {
            var logger = services.GetRequiredService<ILogger<HttpClient>>();
            
            return Polly.Extensions.Http.HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: failureThreshold,
                    durationOfBreak: duration,
                    onBreak: (outcome, breakDuration) =>
                    {
                        var reason = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString();
                        logger.LogWarning(
                            "Circuit breaker opened for {DurationSeconds}s after {FailureCount} failures due to {Reason}",
                            breakDuration.TotalSeconds,
                            failureThreshold,
                            reason);
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("Circuit breaker reset");
                    });
        });
    }

    /// <summary>
    /// Adds a timeout policy to an HttpClient.
    /// </summary>
    /// <param name="builder">The HttpClient builder.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The HttpClient builder for chaining.</returns>
    public static IHttpClientBuilder AddTimeoutPolicy(
        this IHttpClientBuilder builder,
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));
        }

        return builder.AddPolicyHandler((services, request) =>
        {
            var logger = services.GetRequiredService<ILogger<HttpClient>>();
            
            return Policy.TimeoutAsync<HttpResponseMessage>(
                timeout,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    logger.LogWarning(
                        "HTTP request timed out after {TimeoutSeconds}s",
                        timespan.TotalSeconds);
                    return Task.CompletedTask;
                });
        });
    }
}
