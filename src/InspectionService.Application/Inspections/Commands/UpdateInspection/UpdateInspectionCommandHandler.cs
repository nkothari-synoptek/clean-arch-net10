using Common.Shared.Caching;
using InspectionService.Application.Inspections.Interfaces;
using InspectionService.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InspectionService.Application.Inspections.Commands.UpdateInspection;

/// <summary>
/// Handler for UpdateInspectionCommand with cache invalidation
/// </summary>
public class UpdateInspectionCommandHandler : IRequestHandler<UpdateInspectionCommand, Result>
{
    private readonly IInspectionRepository _repository;
    private readonly IDistributedCacheService _cache;
    private readonly ILogger<UpdateInspectionCommandHandler> _logger;
    private static readonly ActivitySource ActivitySource = new("InspectionService.Application");

    public UpdateInspectionCommandHandler(
        IInspectionRepository repository,
        IDistributedCacheService cache,
        ILogger<UpdateInspectionCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> Handle(
        UpdateInspectionCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("UpdateInspection", ActivityKind.Internal);
        activity?.SetTag("inspection.id", request.Id);

        try
        {
            // Retrieve existing inspection
            var result = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Inspection {InspectionId} not found for update: {Error}",
                    request.Id,
                    result.Error);
                return Result.Failure(result.Error);
            }

            var inspection = result.Value;

            // Update inspection details
            inspection.UpdateDetails(request.Title, request.Description);

            // Persist changes
            var updateResult = await _repository.UpdateAsync(inspection, cancellationToken);

            if (!updateResult.IsSuccess)
            {
                _logger.LogError(
                    "Failed to update inspection {InspectionId}: {Error}",
                    request.Id,
                    updateResult.Error);
                activity?.SetStatus(ActivityStatusCode.Error, updateResult.Error);
                return updateResult;
            }

            // Invalidate cache
            var cacheKey = $"inspection:{request.Id}";
            await _cache.RemoveAsync(cacheKey, cancellationToken);

            _logger.LogInformation(
                "Updated inspection {InspectionId} and invalidated cache",
                request.Id);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating inspection {InspectionId}",
                request.Id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result.Failure($"Failed to update inspection: {ex.Message}");
        }
    }
}
