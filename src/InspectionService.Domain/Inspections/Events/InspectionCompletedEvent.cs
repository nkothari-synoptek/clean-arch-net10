using InspectionService.Shared.Kernel.Base;

namespace InspectionService.Domain.Inspections.Events;

/// <summary>
/// Domain event raised when an inspection is completed
/// </summary>
public sealed class InspectionCompletedEvent : IDomainEvent
{
    public Guid InspectionId { get; }
    public string Title { get; }
    public string CompletedBy { get; }
    public DateTime OccurredOn { get; }
    public int CompliantItemsCount { get; }
    public int TotalItemsCount { get; }

    public InspectionCompletedEvent(
        Guid inspectionId,
        string title,
        string completedBy,
        DateTime occurredOn,
        int compliantItemsCount,
        int totalItemsCount)
    {
        InspectionId = inspectionId;
        Title = title;
        CompletedBy = completedBy;
        OccurredOn = occurredOn;
        CompliantItemsCount = compliantItemsCount;
        TotalItemsCount = totalItemsCount;
    }
}
