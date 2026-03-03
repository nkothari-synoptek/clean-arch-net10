using InspectionService.Application.Inspections.Interfaces;
using InspectionService.Domain.Inspections.Entities;
using InspectionService.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InspectionService.Application.Inspections.Commands.CreateInspection;

/// <summary>
/// Handler for CreateInspectionCommand
/// </summary>
public class CreateInspectionCommandHandler : IRequestHandler<CreateInspectionCommand, Result<Guid>>
{
    private readonly IInspectionRepository _repository;
    private readonly ILogger<CreateInspectionCommandHandler> _logger;
    private static readonly ActivitySource ActivitySource = new("InspectionService.Application");

    public CreateInspectionCommandHandler(
        IInspectionRepository repository,
        ILogger<CreateInspectionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(
        CreateInspectionCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("CreateInspection", ActivityKind.Internal);
        
        try
        {
            // Create inspection entity using factory method
            var inspection = Inspection.Create(
                request.Title,
                request.Description,
                request.CreatedBy);

            activity?.SetTag("inspection.id", inspection.Id);
            activity?.SetTag("inspection.title", request.Title);
            activity?.SetTag("inspection.created_by", request.CreatedBy);

            // Add inspection items
            foreach (var itemDto in request.Items)
            {
                var addResult = inspection.AddItem(
                    itemDto.Name,
                    itemDto.Description,
                    itemDto.Order);

                if (!addResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Failed to add item to inspection: {Error}",
                        addResult.Error);
                    return Result.Failure<Guid>(addResult.Error);
                }
            }

            activity?.SetTag("inspection.items.count", request.Items.Count);

            // Persist to repository
            var result = await _repository.AddAsync(inspection, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogError(
                    "Failed to create inspection: {Error}",
                    result.Error);
                activity?.SetStatus(ActivityStatusCode.Error, result.Error);
                return Result.Failure<Guid>(result.Error);
            }

            _logger.LogInformation(
                "Created inspection {InspectionId} with {ItemCount} items by {CreatedBy}",
                inspection.Id,
                request.Items.Count,
                request.CreatedBy);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(inspection.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error creating inspection");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result.Failure<Guid>($"Failed to create inspection: {ex.Message}");
        }
    }
}
