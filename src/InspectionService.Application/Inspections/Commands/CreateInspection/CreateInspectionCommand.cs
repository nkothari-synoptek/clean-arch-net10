using InspectionService.Shared.Kernel.Common;
using MediatR;

namespace InspectionService.Application.Inspections.Commands.CreateInspection;

/// <summary>
/// Command to create a new inspection
/// </summary>
public record CreateInspectionCommand : IRequest<Result<Guid>>
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
    public List<CreateInspectionItemDto> Items { get; init; } = new();
}

/// <summary>
/// DTO for creating inspection items
/// </summary>
public record CreateInspectionItemDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Order { get; init; }
}
