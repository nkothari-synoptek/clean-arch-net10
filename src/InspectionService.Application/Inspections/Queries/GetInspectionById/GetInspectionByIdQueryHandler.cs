using Common.Shared.Caching;
using InspectionService.Application.Inspections.DTOs;
using InspectionService.Application.Inspections.Interfaces;
using InspectionService.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InspectionService.Application.Inspections.Queries.GetInspectionById;

/// <summary>
/// Handler for GetInspectionByIdQuery with caching support
/// </summary>
public class GetInspectionByIdQueryHandler : IRequestHandler<GetInspectionByIdQuery, Result<InspectionDto>>
{
    private readonly IInspectionRepository _repository;
    private readonly IDistributedCacheService _cache;
    private readonly ILogger<GetInspectionByIdQueryHandler> _logger;
    private static readonly ActivitySource ActivitySource = new("InspectionService.Application");

    public GetInspectionByIdQueryHandler(
        IInspectionRepository repository,
        IDistributedCacheService cache,
        ILogger<GetInspectionByIdQueryHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<InspectionDto>> Handle(
        GetInspectionByIdQuery request,
        CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("GetInspectionById", ActivityKind.Internal);
        activity?.SetTag("inspection.id", request.Id);

        var cacheKey = $"inspection:{request.Id}";

        try
        {
            // Try cache first
            var cached = await _cache.GetAsync<InspectionDto>(cacheKey, cancellationToken);
            if (cached != null)
            {
                _logger.LogInformation(
                    "Retrieved inspection {InspectionId} from cache",
                    request.Id);
                activity?.SetTag("cache.hit", true);
                return Result.Success(cached);
            }

            activity?.SetTag("cache.hit", false);

            // Fetch from repository
            var result = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Inspection {InspectionId} not found: {Error}",
                    request.Id,
                    result.Error);
                return Result.Failure<InspectionDto>(result.Error);
            }

            var inspection = result.Value;

            // Map to DTO
            var dto = new InspectionDto
            {
                Id = inspection.Id,
                Title = inspection.Title,
                Description = inspection.Description,
                Status = inspection.Status.ToString(),
                CreatedAt = inspection.CreatedAt,
                CompletedAt = inspection.CompletedAt,
                CreatedBy = inspection.CreatedBy,
                CompletedBy = inspection.CompletedBy,
                Items = inspection.Items.Select(item => new InspectionItemDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    IsCompliant = item.IsCompliant,
                    Notes = item.Notes,
                    Order = item.Order
                }).ToList()
            };

            // Cache for 15 minutes
            await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(15), cancellationToken);

            _logger.LogInformation(
                "Retrieved inspection {InspectionId} from repository and cached",
                request.Id);

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving inspection {InspectionId}",
                request.Id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result.Failure<InspectionDto>($"Failed to retrieve inspection: {ex.Message}");
        }
    }
}
