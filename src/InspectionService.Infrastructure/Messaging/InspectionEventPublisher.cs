using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Common.Shared.ServiceBus;
using InspectionService.Domain.Inspections.Events;
using Microsoft.Extensions.Logging;

namespace InspectionService.Infrastructure.Messaging;

/// <summary>
/// Publisher for inspection domain events to Azure Service Bus
/// </summary>
public class InspectionEventPublisher
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<InspectionEventPublisher> _logger;
    private const string TopicName = "inspection-events";

    public InspectionEventPublisher(
        IMessagePublisher publisher,
        ILogger<InspectionEventPublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Publishes an InspectionCreated event to the service bus
    /// </summary>
    public async Task PublishInspectionCreatedAsync(
        InspectionCreatedEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new ServiceBusMessage
            {
                Subject = "InspectionCreated",
                Body = BinaryData.FromString(JsonSerializer.Serialize(domainEvent)),
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString()
            };

            message.ApplicationProperties.Add("InspectionId", domainEvent.InspectionId.ToString());
            message.ApplicationProperties.Add("EventType", "InspectionCreated");
            message.ApplicationProperties.Add("OccurredOn", domainEvent.OccurredOn.ToString("O"));

            await _publisher.PublishAsync(TopicName, message, cancellationToken);

            _logger.LogInformation(
                "Published InspectionCreated event for inspection {InspectionId}",
                domainEvent.InspectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish InspectionCreated event for inspection {InspectionId}",
                domainEvent.InspectionId);
            throw;
        }
    }

    /// <summary>
    /// Publishes an InspectionCompleted event to the service bus
    /// </summary>
    public async Task PublishInspectionCompletedAsync(
        InspectionCompletedEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new ServiceBusMessage
            {
                Subject = "InspectionCompleted",
                Body = BinaryData.FromString(JsonSerializer.Serialize(domainEvent)),
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString()
            };

            message.ApplicationProperties.Add("InspectionId", domainEvent.InspectionId.ToString());
            message.ApplicationProperties.Add("EventType", "InspectionCompleted");
            message.ApplicationProperties.Add("OccurredOn", domainEvent.OccurredOn.ToString("O"));
            message.ApplicationProperties.Add("CompliantItemsCount", domainEvent.CompliantItemsCount);
            message.ApplicationProperties.Add("TotalItemsCount", domainEvent.TotalItemsCount);

            await _publisher.PublishAsync(TopicName, message, cancellationToken);

            _logger.LogInformation(
                "Published InspectionCompleted event for inspection {InspectionId} with {CompliantItems}/{TotalItems} compliant items",
                domainEvent.InspectionId,
                domainEvent.CompliantItemsCount,
                domainEvent.TotalItemsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish InspectionCompleted event for inspection {InspectionId}",
                domainEvent.InspectionId);
            throw;
        }
    }
}
