using InspectionService.Shared.Kernel.Common;
using MediatR;

namespace InspectionService.Application.Inspections.Commands.DeleteInspection;

/// <summary>
/// Command to delete an inspection
/// </summary>
public record DeleteInspectionCommand(Guid Id) : IRequest<Result>;
