using Common.Shared.Caching;
using InspectionService.Application.Inspections.Interfaces;
using InspectionService.Domain.Inspections.Entities;
using InspectionService.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InspectionService.Infrastructure.Persistence.Repositories.Inspections;

/// <summary>
/// Repository implementation for Inspection aggregate
/// </summary>
public class InspectionRepository : IInspectionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCacheService _cache;
    private readonly ILogger<InspectionRepository> _logger;
    private const string CacheKeyPrefix = "inspection:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

    public InspectionRepository(
        ApplicationDbContext context,
        IDistributedCacheService cache,
        ILogger<InspectionRepository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<Inspection>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: Caching is disabled for domain entities with private backing fields
            // as they don't serialize/deserialize properly with System.Text.Json
            // Consider using DTOs for caching if needed
            
            var inspection = await _context.Inspections
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            if (inspection == null)
            {
                _logger.LogWarning("Inspection {InspectionId} not found", id);
                return Result.Failure<Inspection>($"Inspection with ID {id} not found");
            }

            _logger.LogInformation("Retrieved inspection {InspectionId} from database", id);

            return Result.Success(inspection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inspection {InspectionId}", id);
            return Result.Failure<Inspection>($"Error retrieving inspection: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<Inspection>>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Inspections
                .Include(i => i.Items)
                .AsQueryable();

            // Load all inspections (or apply other filters first if needed)
            var allInspections = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);

            // Apply status filter in memory after loading
            // This is necessary because EF Core cannot translate value object comparisons to SQL
            IEnumerable<Inspection> filteredInspections = allInspections;
            if (!string.IsNullOrWhiteSpace(status))
            {
                filteredInspections = allInspections.Where(i => i.Status.Value == status);
            }

            // Apply pagination in memory
            var paginatedInspections = filteredInspections
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            _logger.LogInformation(
                "Retrieved {Count} inspections (page {PageNumber}, size {PageSize}, status {Status})",
                paginatedInspections.Count,
                pageNumber,
                pageSize,
                status ?? "all");

            return Result.Success<IReadOnlyList<Inspection>>(paginatedInspections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged inspections");
            return Result.Failure<IReadOnlyList<Inspection>>($"Error retrieving inspections: {ex.Message}");
        }
    }

    public async Task<Result<Guid>> AddAsync(Inspection inspection, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Inspections.AddAsync(inspection, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added inspection {InspectionId}", inspection.Id);

            return Result.Success(inspection.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding inspection {InspectionId}", inspection.Id);
            return Result.Failure<Guid>($"Error adding inspection: {ex.Message}");
        }
    }

    public async Task<Result> UpdateAsync(Inspection inspection, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Inspections.Update(inspection);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated inspection {InspectionId}", inspection.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inspection {InspectionId}", inspection.Id);
            return Result.Failure($"Error updating inspection: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var inspection = await _context.Inspections
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            if (inspection == null)
            {
                _logger.LogWarning("Inspection {InspectionId} not found for deletion", id);
                return Result.Failure($"Inspection with ID {id} not found");
            }

            _context.Inspections.Remove(inspection);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted inspection {InspectionId}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inspection {InspectionId}", id);
            return Result.Failure($"Error deleting inspection: {ex.Message}");
        }
    }

    public async Task<int> GetTotalCountAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Load all inspections to filter in memory
            // This is necessary because EF Core cannot translate value object comparisons to SQL
            var allInspections = await _context.Inspections.ToListAsync(cancellationToken);

            int count;
            if (!string.IsNullOrWhiteSpace(status))
            {
                count = allInspections.Count(i => i.Status.Value == status);
            }
            else
            {
                count = allInspections.Count;
            }

            _logger.LogInformation("Total inspection count: {Count} (status: {Status})", count, status ?? "all");

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total inspection count");
            return 0;
        }
    }
}
