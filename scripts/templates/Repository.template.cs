using Common.Shared.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using {{ServiceName}}.Application.{{ModuleName}}.Interfaces;
using {{ServiceName}}.Domain.{{ModuleName}}.Entities;
using {{ServiceName}}.Infrastructure.Persistence;
using {{ServiceName}}.Shared.Kernel.Common;

namespace {{ServiceName}}.Infrastructure.Persistence.Repositories.{{ModuleName}};

/// <summary>
/// Repository implementation for {{EntityName}} entity
/// </summary>
public class {{EntityName}}Repository : I{{EntityName}}Repository
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCacheService _cache;
    private readonly ILogger<{{EntityName}}Repository> _logger;

    public {{EntityName}}Repository(
        ApplicationDbContext context,
        IDistributedCacheService cache,
        ILogger<{{EntityName}}Repository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Gets a {{EntityName}} by its unique identifier
    /// </summary>
    public async Task<Result<{{EntityName}}>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{{entityName}}:{id}";

        // Try cache first
        var cached = await _cache.GetAsync<{{EntityName}}>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogInformation("Retrieved {{EntityName}} {Id} from cache", id);
            return Result<{{EntityName}}>.Success(cached);
        }

        // Fetch from database
        var entity = await _context.{{EntityNamePlural}}
            // Add includes if needed
            // .Include(e => e.RelatedEntity)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity == null)
        {
            _logger.LogWarning("{{EntityName}} with ID {Id} not found", id);
            return Result<{{EntityName}}>.Failure($"{{EntityName}} with ID {id} not found");
        }

        // Cache for 15 minutes
        await _cache.SetAsync(cacheKey, entity, TimeSpan.FromMinutes(15), cancellationToken);

        _logger.LogInformation("Retrieved {{EntityName}} {Id} from database", id);
        return Result<{{EntityName}}>.Success(entity);
    }

    /// <summary>
    /// Gets a paged list of {{EntityNamePlural}}
    /// </summary>
    public async Task<Result<PagedResult<{{EntityName}}>>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? filterProperty,
        CancellationToken cancellationToken = default)
    {
        var query = _context.{{EntityNamePlural}}.AsQueryable();

        // Apply filters
        // if (!string.IsNullOrEmpty(filterProperty))
        // {
        //     query = query.Where(e => e.Property == filterProperty);
        // }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<{{EntityName}}>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        _logger.LogInformation("Retrieved {Count} {{EntityNamePlural}} (page {PageNumber})", items.Count, pageNumber);
        return Result<PagedResult<{{EntityName}}>>.Success(result);
    }

    /// <summary>
    /// Adds a new {{EntityName}} to the database
    /// </summary>
    public async Task<Result<Unit>> AddAsync({{EntityName}} entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.{{EntityNamePlural}}.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added {{EntityName}} with ID {Id}", entity.Id);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add {{EntityName}}");
            return Result<Unit>.Failure($"Failed to add {{EntityName}}: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing {{EntityName}} in the database
    /// </summary>
    public async Task<Result<Unit>> UpdateAsync({{EntityName}} entity, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.{{EntityNamePlural}}.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            var cacheKey = $"{{entityName}}:{entity.Id}";
            await _cache.RemoveAsync(cacheKey, cancellationToken);

            _logger.LogInformation("Updated {{EntityName}} with ID {Id}", entity.Id);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update {{EntityName}} with ID {Id}", entity.Id);
            return Result<Unit>.Failure($"Failed to update {{EntityName}}: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a {{EntityName}} from the database
    /// </summary>
    public async Task<Result<Unit>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.{{EntityNamePlural}}
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("{{EntityName}} with ID {Id} not found for deletion", id);
                return Result<Unit>.Failure($"{{EntityName}} with ID {id} not found");
            }

            _context.{{EntityNamePlural}}.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            var cacheKey = $"{{entityName}}:{id}";
            await _cache.RemoveAsync(cacheKey, cancellationToken);

            _logger.LogInformation("Deleted {{EntityName}} with ID {Id}", id);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {{EntityName}} with ID {Id}", id);
            return Result<Unit>.Failure($"Failed to delete {{EntityName}}: {ex.Message}");
        }
    }
}
