using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Common.Shared.ServiceBus;

/// <summary>
/// Azure Service Bus implementation of message publisher.
/// Follows best practices for connection management and error handling.
/// </summary>
public sealed class ServiceBusPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders;

    public ServiceBusPublisher(
        ServiceBusClient client,
        ILogger<ServiceBusPublisher> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _senders = new ConcurrentDictionary<string, ServiceBusSender>();
    }

    public async Task PublishAsync(
        string topicName,
        ServiceBusMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentNullException.ThrowIfNull(message);

        var sender = GetOrCreateSender(topicName);

        try
        {
            await sender.SendMessageAsync(message, cancellationToken);
            
            _logger.LogInformation(
                "Published message to topic {TopicName} with MessageId: {MessageId}",
                topicName,
                message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish message to topic {TopicName} with MessageId: {MessageId}",
                topicName,
                message.MessageId);
            throw;
        }
    }

    public async Task PublishBatchAsync(
        string topicName,
        IEnumerable<ServiceBusMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentNullException.ThrowIfNull(messages);

        var messageList = messages.ToList();
        if (messageList.Count == 0)
        {
            _logger.LogWarning("Attempted to publish empty batch to topic {TopicName}", topicName);
            return;
        }

        var sender = GetOrCreateSender(topicName);

        try
        {
            await sender.SendMessagesAsync(messageList, cancellationToken);
            
            _logger.LogInformation(
                "Published batch of {MessageCount} messages to topic {TopicName}",
                messageList.Count,
                topicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish batch of {MessageCount} messages to topic {TopicName}",
                messageList.Count,
                topicName);
            throw;
        }
    }

    private ServiceBusSender GetOrCreateSender(string topicName)
    {
        return _senders.GetOrAdd(topicName, name =>
        {
            _logger.LogDebug("Creating new sender for topic {TopicName}", name);
            return _client.CreateSender(name);
        });
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
        
        _senders.Clear();
    }
}
