using Common.Shared.Caching;
using InspectionService.Application.Inspections.Interfaces;
using InspectionService.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InspectionService.Application.Inspections.Commands.DeleteInspection;

/// <summary>
/// Handler for DeleteInspectionCommand with cache invalidation
/// </summary>
public class DeleteInspectionCommandHandler : IRequestHandler<DeleteInspectionCommand, Result>
{
    private readonly IInspectionRepository _repository;
    private readonly IDistributedCacheService _cache;
    private readonly ILogger<DeleteInspectionCommandHandler> _logger;
    private static readonly ActivitySource ActivitySource = new("InspectionService.Application");

    public DeleteInspectionCommandHandler(
        IInspectionRepository repository,
        IDistributedCacheService cache,
        ILogger<DeleteInspectionCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteInspectionCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("DeleteInspection", ActivityKind.Internal);
        activity?.SetTag("inspection.id", request.Id);

        try
        {
            // Verify inspection exists
            var existsResult = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (!existsResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Inspection {InspectionId} not found for deletion: {Error}",
                    request.Id,
                    existsResult.Error);
                return Result.Failure(existsResult.Error);
            }

            // Delete inspection
            var deleteResult = await _repository.DeleteAsync(request.Id, cancellationToken);

            if (!deleteResult.IsSuccess)
            {
                _logger.LogError(
                    "Failed to delete inspection {InspectionId}: {Error}",
                    request.Id,
                    deleteResult.Error);
                activity?.SetStatus(ActivityStatusCode.Error, deleteResult.Error);
                return deleteResult;
            }

            // Invalidate cache
            var cacheKey = $"inspection:{request.Id}";
            await _cache.RemoveAsync(cacheKey, cancellationToken);

            _logger.LogInformation(
                "Deleted inspection {InspectionId} and invalidated cache",
                request.Id);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting inspection {InspectionId}",
                request.Id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result.Failure($"Failed to delete inspection: {ex.Message}");
        }
    }
}
