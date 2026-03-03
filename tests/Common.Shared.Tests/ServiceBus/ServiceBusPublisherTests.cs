using Azure.Messaging.ServiceBus;
using Common.Shared.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Common.Shared.Tests.ServiceBus;

public class ServiceBusPublisherTests
{
    private readonly ServiceBusClient _mockClient;
    private readonly ServiceBusSender _mockSender;
    private readonly ILogger<ServiceBusPublisher> _mockLogger;
    private readonly ServiceBusPublisher _sut;

    public ServiceBusPublisherTests()
    {
        _mockClient = Substitute.For<ServiceBusClient>();
        _mockSender = Substitute.For<ServiceBusSender>();
        _mockLogger = Substitute.For<ILogger<ServiceBusPublisher>>();

        _mockClient.CreateSender(Arg.Any<string>()).Returns(_mockSender);

        _sut = new ServiceBusPublisher(_mockClient, _mockLogger);
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_SendsMessageSuccessfully()
    {
        // Arrange
        var topicName = "test-topic";
        var message = new ServiceBusMessage("test message")
        {
            MessageId = "msg-123"
        };

        // Act
        await _sut.PublishAsync(topicName, message);

        // Assert
        await _mockSender.Received(1).SendMessageAsync(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithNullOrWhiteSpaceTopicName_ThrowsArgumentException()
    {
        // Arrange
        var message = new ServiceBusMessage("test message");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.PublishAsync("", message));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.PublishAsync("   ", message));
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var topicName = "test-topic";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.PublishAsync(topicName, null!));
    }

    [Fact]
    public async Task PublishAsync_WhenSendFails_ThrowsException()
    {
        // Arrange
        var topicName = "test-topic";
        var message = new ServiceBusMessage("test message")
        {
            MessageId = "msg-123"
        };
        var expectedException = new ServiceBusException("Send failed", ServiceBusFailureReason.ServiceTimeout);
        _mockSender.SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>())
            .Throws(expectedException);

        // Act
        var act = async () => await _sut.PublishAsync(topicName, message);

        // Assert
        await act.Should().ThrowAsync<ServiceBusException>();
    }

    [Fact]
    public async Task PublishAsync_ReusesSenderForSameTopic()
    {
        // Arrange
        var topicName = "test-topic";
        var message1 = new ServiceBusMessage("message 1");
        var message2 = new ServiceBusMessage("message 2");

        // Act
        await _sut.PublishAsync(topicName, message1);
        await _sut.PublishAsync(topicName, message2);

        // Assert
        _mockClient.Received(1).CreateSender(topicName);
        await _mockSender.Received(2).SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishBatchAsync_WithValidMessages_SendsBatchSuccessfully()
    {
        // Arrange
        var topicName = "test-topic";
        var messages = new[]
        {
            new ServiceBusMessage("message 1"),
            new ServiceBusMessage("message 2"),
            new ServiceBusMessage("message 3")
        };

        // Act
        await _sut.PublishBatchAsync(topicName, messages);

        // Assert
        await _mockSender.Received(1).SendMessagesAsync(
            Arg.Is<IEnumerable<ServiceBusMessage>>(m => m.Count() == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishBatchAsync_WithNullOrWhiteSpaceTopicName_ThrowsArgumentException()
    {
        // Arrange
        var messages = new[] { new ServiceBusMessage("test") };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.PublishBatchAsync("", messages));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.PublishBatchAsync("   ", messages));
    }

    [Fact]
    public async Task PublishBatchAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        // Arrange
        var topicName = "test-topic";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.PublishBatchAsync(topicName, null!));
    }

    [Fact]
    public async Task PublishBatchAsync_WithEmptyMessages_DoesNotSend()
    {
        // Arrange
        var topicName = "test-topic";
        var messages = Array.Empty<ServiceBusMessage>();

        // Act
        await _sut.PublishBatchAsync(topicName, messages);

        // Assert
        await _mockSender.DidNotReceive().SendMessagesAsync(Arg.Any<IEnumerable<ServiceBusMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishBatchAsync_WhenSendFails_ThrowsException()
    {
        // Arrange
        var topicName = "test-topic";
        var messages = new[] { new ServiceBusMessage("test") };
        var expectedException = new ServiceBusException("Batch send failed", ServiceBusFailureReason.ServiceTimeout);
        _mockSender.SendMessagesAsync(Arg.Any<IEnumerable<ServiceBusMessage>>(), Arg.Any<CancellationToken>())
            .Throws(expectedException);

        // Act
        var act = async () => await _sut.PublishBatchAsync(topicName, messages);

        // Assert
        await act.Should().ThrowAsync<ServiceBusException>();
    }

    [Fact]
    public async Task DisposeAsync_DisposesAllSenders()
    {
        // Arrange
        var topic1 = "topic-1";
        var topic2 = "topic-2";
        var message = new ServiceBusMessage("test");

        // Create senders for multiple topics
        await _sut.PublishAsync(topic1, message);
        await _sut.PublishAsync(topic2, message);

        // Act
        await _sut.DisposeAsync();

        // Assert
        await _mockSender.Received(2).DisposeAsync();
    }
}
