using Azure.Messaging.ServiceBus;

namespace Common.Shared.ServiceBus;

/// <summary>
/// Defines a contract for publishing messages to Azure Service Bus.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a single message to the specified topic.
    /// </summary>
    /// <param name="topicName">The name of the topic.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(string topicName, ServiceBusMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a batch of messages to the specified topic.
    /// </summary>
    /// <param name="topicName">The name of the topic.</param>
    /// <param name="messages">The messages to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishBatchAsync(string topicName, IEnumerable<ServiceBusMessage> messages, CancellationToken cancellationToken = default);
}
