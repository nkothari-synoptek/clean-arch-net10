using Common.Shared.Caching;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace Common.Shared.Tests.Caching;

public class RedisCacheServiceTests
{
    private readonly IConnectionMultiplexer _mockRedis;
    private readonly IDatabase _mockDatabase;
    private readonly ILogger<RedisCacheService> _mockLogger;
    private readonly RedisCacheService _sut;

    public RedisCacheServiceTests()
    {
        _mockRedis = Substitute.For<IConnectionMultiplexer>();
        _mockDatabase = Substitute.For<IDatabase>();
        _mockLogger = Substitute.For<ILogger<RedisCacheService>>();

        _mockRedis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(_mockDatabase);

        _sut = new RedisCacheService(_mockRedis, _mockLogger);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ReturnsDeserializedValue()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = new TestData { Id = 1, Name = "Test" };
        var serialized = JsonSerializer.Serialize(expectedValue);
        _mockDatabase.StringGetAsync(key, Arg.Any<CommandFlags>())
            .Returns(new RedisValue(serialized));

        // Act
        var result = await _sut.GetAsync<TestData>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedValue.Id);
        result.Name.Should().Be(expectedValue.Name);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsDefault()
    {
        // Arrange
        var key = "non-existent-key";
        _mockDatabase.StringGetAsync(key, Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);

        // Act
        var result = await _sut.GetAsync<TestData>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenExceptionOccurs_ReturnsDefault()
    {
        // Arrange
        var key = "error-key";
        _mockDatabase.StringGetAsync(key, Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act
        var result = await _sut.GetAsync<TestData>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithNullOrWhiteSpaceKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetAsync<TestData>(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetAsync<TestData>("   "));
    }

    [Fact]
    public async Task SetAsync_WithNullOrWhiteSpaceKey_ThrowsArgumentException()
    {
        // Arrange
        var value = new TestData { Id = 1, Name = "Test" };
        var expiration = TimeSpan.FromMinutes(15);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SetAsync("", value, expiration));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SetAsync("   ", value, expiration));
    }

    [Fact]
    public async Task SetAsync_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var key = "test-key";
        var expiration = TimeSpan.FromMinutes(15);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.SetAsync<TestData>(key, null!, expiration));
    }

    [Fact]
    public async Task SetAsync_WithZeroOrNegativeExpiration_ThrowsArgumentException()
    {
        // Arrange
        var key = "test-key";
        var value = new TestData { Id = 1, Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SetAsync(key, value, TimeSpan.Zero));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SetAsync(key, value, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public async Task SetAsync_WhenExceptionOccurs_DoesNotThrow()
    {
        // Arrange
        var key = "error-key";
        var value = new TestData { Id = 1, Name = "Test" };
        var expiration = TimeSpan.FromMinutes(15);
        _mockDatabase.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act
        var act = async () => await _sut.SetAsync(key, value, expiration);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_WithValidKey_DeletesKey()
    {
        // Arrange
        var key = "test-key";

        // Act
        await _sut.RemoveAsync(key);

        // Assert
        await _mockDatabase.Received(1).KeyDeleteAsync(key, Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RemoveAsync_WithNullOrWhiteSpaceKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.RemoveAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.RemoveAsync("   "));
    }

    [Fact]
    public async Task RemoveAsync_WhenExceptionOccurs_DoesNotThrow()
    {
        // Arrange
        var key = "error-key";
        _mockDatabase.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act
        var act = async () => await _sut.RemoveAsync(key);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        var key = "existing-key";
        _mockDatabase.KeyExistsAsync(key, Arg.Any<CommandFlags>()).Returns(true);

        // Act
        var result = await _sut.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var key = "non-existent-key";
        _mockDatabase.KeyExistsAsync(key, Arg.Any<CommandFlags>()).Returns(false);

        // Act
        var result = await _sut.ExistsAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithNullOrWhiteSpaceKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.ExistsAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.ExistsAsync("   "));
    }

    [Fact]
    public async Task ExistsAsync_WhenExceptionOccurs_ReturnsFalse()
    {
        // Arrange
        var key = "error-key";
        _mockDatabase.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act
        var result = await _sut.ExistsAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
