using InspectionService.Application.Common.Models;
using InspectionService.Application.Inspections.DTOs;
using InspectionService.Shared.Kernel.Common;
using MediatR;

namespace InspectionService.Application.Inspections.Queries.ListInspections;

/// <summary>
/// Query to list inspections with pagination
/// </summary>
public record ListInspectionsQuery : IRequest<Result<PagedResult<InspectionSummaryDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Status { get; init; }
}
