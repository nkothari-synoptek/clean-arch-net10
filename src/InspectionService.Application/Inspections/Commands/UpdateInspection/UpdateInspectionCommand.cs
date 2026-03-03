using InspectionService.Shared.Kernel.Common;
using MediatR;

namespace InspectionService.Application.Inspections.Commands.UpdateInspection;

/// <summary>
/// Command to update an existing inspection
/// </summary>
public record UpdateInspectionCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
