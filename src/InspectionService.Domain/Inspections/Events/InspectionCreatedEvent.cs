using InspectionService.Shared.Kernel.Base;

namespace InspectionService.Domain.Inspections.Events;

/// <summary>
/// Domain event raised when an inspection is created
/// </summary>
public sealed class InspectionCreatedEvent : IDomainEvent
{
    public Guid InspectionId { get; }
    public string Title { get; }
    public string CreatedBy { get; }
    public DateTime OccurredOn { get; }

    public InspectionCreatedEvent(
        Guid inspectionId,
        string title,
        string createdBy,
        DateTime occurredOn)
    {
        InspectionId = inspectionId;
        Title = title;
        CreatedBy = createdBy;
        OccurredOn = occurredOn;
    }
}
