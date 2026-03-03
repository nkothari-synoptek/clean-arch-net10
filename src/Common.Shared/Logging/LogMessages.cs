using Microsoft.Extensions.Logging;

namespace Common.Shared.Logging;

/// <summary>
/// High-performance logging delegates using LoggerMessage.Define.
/// Provides compile-time generated logging methods for optimal performance.
/// </summary>
public static partial class LogMessages
{
    // Cache Operations
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Cache hit for key: {CacheKey}")]
    public static partial void LogCacheHit(this ILogger logger, string cacheKey);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Debug,
        Message = "Cache miss for key: {CacheKey}")]
    public static partial void LogCacheMiss(this ILogger logger, string cacheKey);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Cache operation failed for key: {CacheKey}")]
    public static partial void LogCacheOperationFailed(this ILogger logger, Exception exception, string cacheKey);

    // Service Bus Operations
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Published message to topic {TopicName} with MessageId: {MessageId}")]
    public static partial void LogMessagePublished(this ILogger logger, string topicName, string messageId);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Error,
        Message = "Failed to publish message to topic {TopicName} with MessageId: {MessageId}")]
    public static partial void LogMessagePublishFailed(this ILogger logger, Exception exception, string topicName, string messageId);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Information,
        Message = "Started consuming messages from topic {TopicName}, subscription {SubscriptionName}")]
    public static partial void LogConsumerStarted(this ILogger logger, string topicName, string subscriptionName);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Error,
        Message = "Error processing message from {EntityPath}. Error source: {ErrorSource}")]
    public static partial void LogMessageProcessingError(this ILogger logger, Exception exception, string entityPath, string errorSource);

    // HTTP Client Operations
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Warning,
        Message = "HTTP request retry {RetryCount} after {DelayMs}ms due to {Reason}")]
    public static partial void LogHttpRetry(this ILogger logger, int retryCount, double delayMs, string reason);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "Circuit breaker opened for {DurationSeconds}s due to {Reason}")]
    public static partial void LogCircuitBreakerOpened(this ILogger logger, double durationSeconds, string reason);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Information,
        Message = "Circuit breaker reset")]
    public static partial void LogCircuitBreakerReset(this ILogger logger);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Warning,
        Message = "HTTP request timed out after {TimeoutSeconds}s")]
    public static partial void LogHttpTimeout(this ILogger logger, double timeoutSeconds);

    // Database Operations
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Information,
        Message = "Database query executed in {ElapsedMs}ms")]
    public static partial void LogDatabaseQueryExecuted(this ILogger logger, double elapsedMs);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Error,
        Message = "Database operation failed")]
    public static partial void LogDatabaseOperationFailed(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Warning,
        Message = "Slow database query detected: {ElapsedMs}ms")]
    public static partial void LogSlowDatabaseQuery(this ILogger logger, double elapsedMs);

    // Authentication Operations
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Information,
        Message = "Service token acquired for scope: {Scope}")]
    public static partial void LogServiceTokenAcquired(this ILogger logger, string scope);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Error,
        Message = "Failed to acquire service token for scope: {Scope}")]
    public static partial void LogServiceTokenAcquisitionFailed(this ILogger logger, Exception exception, string scope);

    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Warning,
        Message = "Authentication failed for user: {UserId}")]
    public static partial void LogAuthenticationFailed(this ILogger logger, string userId);

    // General Infrastructure Operations
    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Information,
        Message = "Infrastructure service {ServiceName} started")]
    public static partial void LogInfrastructureServiceStarted(this ILogger logger, string serviceName);

    [LoggerMessage(
        EventId = 6002,
        Level = LogLevel.Information,
        Message = "Infrastructure service {ServiceName} stopped")]
    public static partial void LogInfrastructureServiceStopped(this ILogger logger, string serviceName);

    [LoggerMessage(
        EventId = 6003,
        Level = LogLevel.Error,
        Message = "Infrastructure service {ServiceName} encountered an error")]
    public static partial void LogInfrastructureServiceError(this ILogger logger, Exception exception, string serviceName);

    // Health Check Operations
    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Warning,
        Message = "Health check {HealthCheckName} is unhealthy: {Reason}")]
    public static partial void LogHealthCheckUnhealthy(this ILogger logger, string healthCheckName, string reason);

    [LoggerMessage(
        EventId = 7002,
        Level = LogLevel.Warning,
        Message = "Health check {HealthCheckName} is degraded: {Reason}")]
    public static partial void LogHealthCheckDegraded(this ILogger logger, string healthCheckName, string reason);
}
