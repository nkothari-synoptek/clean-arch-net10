namespace InspectionService.Application.Inspections.DTOs;

/// <summary>
/// DTO for inspection summary in list views
/// </summary>
public record InspectionSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public int ItemCount { get; init; }
}
