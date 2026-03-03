using InspectionService.Domain.Inspections.Entities;
using InspectionService.Shared.Kernel.Common;

namespace InspectionService.Application.Inspections.Interfaces;

/// <summary>
/// Repository interface for Inspection aggregate
/// </summary>
public interface IInspectionRepository
{
    Task<Result<Inspection>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<Result<IReadOnlyList<Inspection>>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default);
    
    Task<Result<Guid>> AddAsync(Inspection inspection, CancellationToken cancellationToken = default);
    
    Task<Result> UpdateAsync(Inspection inspection, CancellationToken cancellationToken = default);
    
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<int> GetTotalCountAsync(string? status = null, CancellationToken cancellationToken = default);
}
