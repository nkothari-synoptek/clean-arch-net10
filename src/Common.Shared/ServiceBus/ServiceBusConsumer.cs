using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Common.Shared.ServiceBus;

/// <summary>
/// Azure Service Bus implementation of message consumer.
/// Follows best practices for message processing and error handling.
/// </summary>
public sealed class ServiceBusConsumer : IMessageConsumer, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusConsumer> _logger;
    private ServiceBusProcessor? _processor;

    public ServiceBusConsumer(
        ServiceBusClient client,
        ILogger<ServiceBusConsumer> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(
        string topicName,
        string subscriptionName,
        Func<ProcessMessageEventArgs, Task> messageHandler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
        ArgumentNullException.ThrowIfNull(messageHandler);

        if (_processor != null)
        {
            throw new InvalidOperationException("Consumer is already started.");
        }

        var options = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1,
            PrefetchCount = 0
        };

        _processor = _client.CreateProcessor(topicName, subscriptionName, options);

        _processor.ProcessMessageAsync += messageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        await _processor.StartProcessingAsync(cancellationToken);
        
        _logger.LogInformation(
            "Started consuming messages from topic {TopicName}, subscription {SubscriptionName}",
            topicName,
            subscriptionName);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_processor == null)
        {
            return;
        }

        await _processor.StopProcessingAsync(cancellationToken);
        
        _logger.LogInformation("Stopped consuming messages");
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Error processing message from {EntityPath}. Error source: {ErrorSource}",
            args.EntityPath,
            args.ErrorSource);

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_processor != null)
        {
            await _processor.DisposeAsync();
            _processor = null;
        }
    }
}
