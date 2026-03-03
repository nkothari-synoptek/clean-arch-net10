namespace InspectionService.Application.Inspections.DTOs;

/// <summary>
/// DTO for inspection details
/// </summary>
public record InspectionDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string? CompletedBy { get; init; }
    public List<InspectionItemDto> Items { get; init; } = new();
}

/// <summary>
/// DTO for inspection item details
/// </summary>
public record InspectionItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsCompliant { get; init; }
    public string? Notes { get; init; }
    public int Order { get; init; }
}
