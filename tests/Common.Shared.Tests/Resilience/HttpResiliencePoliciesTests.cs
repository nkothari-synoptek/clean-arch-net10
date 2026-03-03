using Common.Shared.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly;
using Polly.Timeout;
using System.Net;
using Xunit;

namespace Common.Shared.Tests.Resilience;

public class HttpResiliencePoliciesTests
{
    private readonly ILogger _mockLogger;

    public HttpResiliencePoliciesTests()
    {
        _mockLogger = Substitute.For<ILogger>();
    }

    [Fact]
    public async Task GetRetryPolicy_WithTransientError_RetriesRequest()
    {
        // Arrange
        var policy = HttpResiliencePolicies.GetRetryPolicy();
        var attemptCount = 0;
        var context = new Context { ["Logger"] = _mockLogger };

        // Act
        var result = await policy.ExecuteAsync(async (ctx) =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new HttpRequestException("Transient error");
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, context);

        // Assert
        attemptCount.Should().Be(3);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRetryPolicy_WithTooManyRequests_RetriesRequest()
    {
        // Arrange
        var policy = HttpResiliencePolicies.GetRetryPolicy();
        var attemptCount = 0;
        var context = new Context { ["Logger"] = _mockLogger };

        // Act
        var result = await policy.ExecuteAsync(async (ctx) =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, context);

        // Assert
        attemptCount.Should().Be(3);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRetryPolicy_ExhaustsRetries_ThrowsException()
    {
        // Arrange
        var policy = HttpResiliencePolicies.GetRetryPolicy();
        var attemptCount = 0;
        var context = new Context { ["Logger"] = _mockLogger };

        // Act
        var act = async () => await policy.ExecuteAsync(async (ctx) =>
        {
            attemptCount++;
            throw new HttpRequestException("Persistent error");
        }, context);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
        attemptCount.Should().Be(4); // Initial attempt + 3 retries
    }

    [Fact]
    public async Task GetRetryPolicy_WithSuccessfulRequest_DoesNotRetry()
    {
        // Arrange
        var policy = HttpResiliencePolicies.GetRetryPolicy();
        var attemptCount = 0;
        var context = new Context { ["Logger"] = _mockLogger };

        // Act
        var result = await policy.ExecuteAsync(async (ctx) =>
        {
            attemptCount++;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, context);

        // Assert
        attemptCount.Should().Be(1);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCircuitBreakerPolicy_OpensAfterConsecutiveFailures()
    {
        // Arrange
        var policy = HttpResiliencePolicies.GetCircuitBreakerPolicy();
        var context = new Context { ["Logger"] = _mockLogger };

        // Act - Trigger 5 failures to open circuit
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await policy.ExecuteAsync(async (ctx) =>
                {
                    throw new HttpRequestException("Service unavailable");
                }, context);
            }
            catch (HttpRequestException)
            {
                // Expected
            }
        }

        // Circuit should now be open
        var act = async () => await policy.ExecuteAsync(async (ctx) =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, context);

        // Assert
        await act.Should().ThrowAsync<Polly.CircuitBreaker.BrokenCircuitException>();
    }

    [Fact]
    public async Task GetCircuitBreakerPolicy_WithSuccessfulRequests_RemainsOpen()
    {
        // Arrange
        var policy = HttpResiliencePolicies.GetCircuitBreakerPolicy();
        var attemptCount = 0;
        var context = new Context { ["Logger"] = _mockLogger };

        // Act - Execute multiple successful requests
        for (int i = 0; i < 10; i++)
        {
            var result = await policy.ExecuteAsync(async (ctx) =>
            {
                attemptCount++;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }, context);

            result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Assert
        attemptCount.Should().Be(10);
    }

    [Fact]
    public async Task GetTimeoutPolicy_WithSlowRequest_ThrowsTimeoutException()
    {
        // Arrange
        var policy = HttpResiliencePolicies.GetTimeoutPolicy();
        var context = new Context { ["Logger"] = _mockLogger };

        // Act
        var act = async () => await policy.ExecuteAsync(async (ctx, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(15), ct); // Longer than 10s timeout
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, context, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task GetTimeoutPolicy_WithFastRequest_CompletesSuccessfully()
    {
        // Arrange
        var policy = HttpResiliencePolicies.GetTimeoutPolicy();
        var context = new Context { ["Logger"] = _mockLogger };

        // Act
        var result = await policy.ExecuteAsync(async (ctx, ct) =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, context, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CombinedPolicies_ApplyInCorrectOrder()
    {
        // Arrange
        var retryPolicy = HttpResiliencePolicies.GetRetryPolicy();
        var timeoutPolicy = HttpResiliencePolicies.GetTimeoutPolicy();
        var combinedPolicy = Policy.WrapAsync(retryPolicy, timeoutPolicy);
        
        var attemptCount = 0;
        var context = new Context { ["Logger"] = _mockLogger };

        // Act
        var result = await combinedPolicy.ExecuteAsync(async (ctx) =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new HttpRequestException("Transient error");
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, context);

        // Assert
        attemptCount.Should().Be(2);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
