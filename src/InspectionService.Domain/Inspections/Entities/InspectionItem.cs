using InspectionService.Shared.Kernel.Base;
using InspectionService.Shared.Kernel.Common;

namespace InspectionService.Domain.Inspections.Entities;

/// <summary>
/// Represents an individual item within an inspection
/// </summary>
public class InspectionItem : Entity
{
    public Guid InspectionId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsCompliant { get; private set; }
    public string? Notes { get; private set; }
    public int Order { get; private set; }

    private InspectionItem(
        Guid id,
        Guid inspectionId,
        string name,
        string description,
        int order)
        : base(id)
    {
        InspectionId = inspectionId;
        Name = name;
        Description = description;
        Order = order;
        IsCompliant = false;
    }

    private InspectionItem() : base() { }

    public static InspectionItem Create(
        Guid inspectionId,
        string name,
        string description,
        int order)
    {
        Guard.AgainstEmptyGuid(inspectionId, nameof(inspectionId));
        Guard.AgainstNullOrEmpty(name, nameof(name));
        Guard.AgainstNullOrEmpty(description, nameof(description));
        Guard.AgainstNegativeOrZero(order, nameof(order));

        return new InspectionItem(
            Guid.NewGuid(),
            inspectionId,
            name,
            description,
            order);
    }

    public void MarkAsCompliant(string? notes = null)
    {
        IsCompliant = true;
        Notes = notes;
    }

    public void MarkAsNonCompliant(string? notes = null)
    {
        IsCompliant = false;
        Notes = notes;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes;
    }
}
