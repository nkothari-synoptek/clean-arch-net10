using InspectionService.Application.Inspections.DTOs;
using InspectionService.Shared.Kernel.Common;
using MediatR;

namespace InspectionService.Application.Inspections.Queries.GetInspectionById;

/// <summary>
/// Query to get an inspection by ID
/// </summary>
public record GetInspectionByIdQuery(Guid Id) : IRequest<Result<InspectionDto>>;
