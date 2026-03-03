using InspectionService.Domain.Inspections.Events;
using InspectionService.Domain.Inspections.ValueObjects;
using InspectionService.Shared.Kernel.Base;
using InspectionService.Shared.Kernel.Common;

namespace InspectionService.Domain.Inspections.Entities;

/// <summary>
/// Represents an inspection aggregate root
/// </summary>
public class Inspection : Entity
{
    private readonly List<InspectionItem> _items = new();

    public string Title { get; private set; }
    public string Description { get; private set; }
    public InspectionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string CreatedBy { get; private set; }
    public string? CompletedBy { get; private set; }

    public IReadOnlyCollection<InspectionItem> Items => _items.AsReadOnly();

    private Inspection(
        Guid id,
        string title,
        string description,
        string createdBy)
        : base(id)
    {
        Title = title;
        Description = description;
        Status = InspectionStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    private Inspection() : base() { }

    public static Inspection Create(
        string title,
        string description,
        string createdBy)
    {
        Guard.AgainstNullOrEmpty(title, nameof(title));
        Guard.AgainstNullOrEmpty(description, nameof(description));
        Guard.AgainstNullOrEmpty(createdBy, nameof(createdBy));

        var inspection = new Inspection(
            Guid.NewGuid(),
            title,
            description,
            createdBy);

        inspection.AddDomainEvent(new InspectionCreatedEvent(
            inspection.Id,
            inspection.Title,
            inspection.CreatedBy,
            inspection.CreatedAt));

        return inspection;
    }

    public Result AddItem(string name, string description, int order)
    {
        if (Status.IsCompleted || Status.IsCancelled)
            return Result.Failure("Cannot add items to a completed or cancelled inspection.");

        var item = InspectionItem.Create(Id, name, description, order);
        _items.Add(item);

        return Result.Success();
    }

    public Result RemoveItem(Guid itemId)
    {
        if (Status.IsCompleted || Status.IsCancelled)
            return Result.Failure("Cannot remove items from a completed or cancelled inspection.");

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            return Result.Failure($"Item with ID {itemId} not found.");

        _items.Remove(item);
        return Result.Success();
    }

    public Result Start()
    {
        if (!Status.IsDraft)
            return Result.Failure("Only draft inspections can be started.");

        if (_items.Count == 0)
            return Result.Failure("Cannot start an inspection without items.");

        Status = InspectionStatus.InProgress;
        return Result.Success();
    }

    public Result Complete(string completedBy)
    {
        Guard.AgainstNullOrEmpty(completedBy, nameof(completedBy));

        if (Status.IsCompleted)
            return Result.Failure("Inspection is already completed.");

        if (Status.IsCancelled)
            return Result.Failure("Cannot complete a cancelled inspection.");

        if (_items.Count == 0)
            return Result.Failure("Cannot complete an inspection without items.");

        var allItemsReviewed = _items.All(i => i.IsCompliant || !string.IsNullOrEmpty(i.Notes));
        if (!allItemsReviewed)
            return Result.Failure("All items must be reviewed before completing the inspection.");

        Status = InspectionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        CompletedBy = completedBy;

        AddDomainEvent(new InspectionCompletedEvent(
            Id,
            Title,
            CompletedBy,
            CompletedAt.Value,
            _items.Count(i => i.IsCompliant),
            _items.Count));

        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status.IsCompleted)
            return Result.Failure("Cannot cancel a completed inspection.");

        if (Status.IsCancelled)
            return Result.Failure("Inspection is already cancelled.");

        Status = InspectionStatus.Cancelled;
        return Result.Success();
    }

    public void UpdateDetails(string title, string description)
    {
        Guard.AgainstNullOrEmpty(title, nameof(title));
        Guard.AgainstNullOrEmpty(description, nameof(description));

        Title = title;
        Description = description;
    }
}
