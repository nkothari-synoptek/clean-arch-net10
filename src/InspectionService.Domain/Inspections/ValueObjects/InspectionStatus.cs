using InspectionService.Shared.Kernel.Base;

namespace InspectionService.Domain.Inspections.ValueObjects;

/// <summary>
/// Value object representing the status of an inspection
/// </summary>
public sealed class InspectionStatus : ValueObject
{
    public string Value { get; }

    private InspectionStatus(string value)
    {
        Value = value;
    }

    public static InspectionStatus Draft => new("Draft");
    public static InspectionStatus InProgress => new("InProgress");
    public static InspectionStatus Completed => new("Completed");
    public static InspectionStatus Cancelled => new("Cancelled");

    public bool IsDraft => Value == Draft.Value;
    public bool IsInProgress => Value == InProgress.Value;
    public bool IsCompleted => Value == Completed.Value;
    public bool IsCancelled => Value == Cancelled.Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
