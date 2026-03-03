namespace InspectionService.Shared.Kernel.Base;

/// <summary>
/// Marker interface for domain events
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
