using {{ServiceName}}.Shared.Kernel.Base;

namespace {{ServiceName}}.Domain.{{ModuleName}}.Entities;

/// <summary>
/// Represents a {{EntityName}} entity in the domain
/// </summary>
public class {{EntityName}} : Entity
{
    /// <summary>
    /// Gets the unique identifier for this {{EntityName}}
    /// </summary>
    public Guid Id { get; private set; }

    // Add your properties here
    // Example:
    // public string Name { get; private set; }
    // public string Description { get; private set; }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private {{EntityName}}()
    {
    }

    /// <summary>
    /// Factory method to create a new {{EntityName}}
    /// </summary>
    /// <returns>A new {{EntityName}} instance</returns>
    public static {{EntityName}} Create(/* Add parameters here */)
    {
        // Add validation logic here
        // Guard.Against.NullOrEmpty(name, nameof(name));

        var entity = new {{EntityName}}
        {
            Id = Guid.NewGuid(),
            // Initialize properties here
        };

        // Add domain event if needed
        // entity.AddDomainEvent(new {{EntityName}}CreatedEvent(entity.Id));

        return entity;
    }

    /// <summary>
    /// Updates the {{EntityName}} properties
    /// </summary>
    public void Update(/* Add parameters here */)
    {
        // Add validation logic here
        // Guard.Against.NullOrEmpty(name, nameof(name));

        // Update properties here
        // this.Name = name;

        // Add domain event if needed
        // AddDomainEvent(new {{EntityName}}UpdatedEvent(Id));
    }

    // Add domain methods here
    // Example:
    // public void Activate()
    // {
    //     IsActive = true;
    //     AddDomainEvent(new {{EntityName}}ActivatedEvent(Id));
    // }
}
