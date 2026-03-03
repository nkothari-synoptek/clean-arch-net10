using InspectionService.Application.Common.Models;
using InspectionService.Application.Inspections.DTOs;
using InspectionService.Application.Inspections.Interfaces;
using InspectionService.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InspectionService.Application.Inspections.Queries.ListInspections;

/// <summary>
/// Handler for ListInspectionsQuery
/// </summary>
public class ListInspectionsQueryHandler : IRequestHandler<ListInspectionsQuery, Result<PagedResult<InspectionSummaryDto>>>
{
    private readonly IInspectionRepository _repository;
    private readonly ILogger<ListInspectionsQueryHandler> _logger;
    private static readonly ActivitySource ActivitySource = new("InspectionService.Application");

    public ListInspectionsQueryHandler(
        IInspectionRepository repository,
        ILogger<ListInspectionsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<InspectionSummaryDto>>> Handle(
        ListInspectionsQuery request,
        CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("ListInspections", ActivityKind.Internal);
        activity?.SetTag("page.number", request.PageNumber);
        activity?.SetTag("page.size", request.PageSize);
        activity?.SetTag("filter.status", request.Status ?? "all");

        try
        {
            // Validate pagination parameters
            if (request.PageNumber < 1)
            {
                return Result.Failure<PagedResult<InspectionSummaryDto>>("Page number must be greater than 0");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return Result.Failure<PagedResult<InspectionSummaryDto>>("Page size must be between 1 and 100");
            }

            // Get paged inspections
            var result = await _repository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.Status,
                cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogError(
                    "Failed to retrieve inspections: {Error}",
                    result.Error);
                return Result.Failure<PagedResult<InspectionSummaryDto>>(result.Error);
            }

            // Get total count
            var totalCount = await _repository.GetTotalCountAsync(request.Status, cancellationToken);

            // Map to summary DTOs
            var summaries = result.Value.Select(inspection => new InspectionSummaryDto
            {
                Id = inspection.Id,
                Title = inspection.Title,
                Status = inspection.Status.ToString(),
                CreatedAt = inspection.CreatedAt,
                CompletedAt = inspection.CompletedAt,
                CreatedBy = inspection.CreatedBy,
                ItemCount = inspection.Items.Count
            }).ToList();

            var pagedResult = new PagedResult<InspectionSummaryDto>
            {
                Items = summaries,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            _logger.LogInformation(
                "Retrieved {Count} inspections (page {PageNumber} of {TotalPages})",
                summaries.Count,
                request.PageNumber,
                pagedResult.TotalPages);

            activity?.SetTag("result.count", summaries.Count);
            activity?.SetTag("result.total_count", totalCount);

            return Result.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error listing inspections");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result.Failure<PagedResult<InspectionSummaryDto>>($"Failed to list inspections: {ex.Message}");
        }
    }
}
