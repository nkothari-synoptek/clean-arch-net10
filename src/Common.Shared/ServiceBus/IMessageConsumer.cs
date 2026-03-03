using Azure.Messaging.ServiceBus;

namespace Common.Shared.ServiceBus;

/// <summary>
/// Defines a contract for consuming messages from Azure Service Bus.
/// </summary>
public interface IMessageConsumer
{
    /// <summary>
    /// Starts consuming messages from the specified topic and subscription.
    /// </summary>
    /// <param name="topicName">The name of the topic.</param>
    /// <param name="subscriptionName">The name of the subscription.</param>
    /// <param name="messageHandler">The handler to process received messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(
        string topicName,
        string subscriptionName,
        Func<ProcessMessageEventArgs, Task> messageHandler,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops consuming messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}
