using MediatR;
using Microsoft.Extensions.Logging;
using {{ServiceName}}.Application.{{ModuleName}}.DTOs;
using {{ServiceName}}.Application.{{ModuleName}}.Interfaces;
using {{ServiceName}}.Shared.Kernel.Common;

namespace {{ServiceName}}.Application.{{ModuleName}}.Queries.{{QueryName}};

/// <summary>
/// Query to {{QueryDescription}}
/// </summary>
public record {{QueryName}}Query : IRequest<Result<{{ReturnType}}>>
{
    // Add query properties here
    // Example for GetById:
    // public Guid Id { get; init; }
    
    // Example for List with pagination:
    // public int PageNumber { get; init; } = 1;
    // public int PageSize { get; init; } = 10;
    // public string? FilterProperty { get; init; }
}

/// <summary>
/// Handler for {{QueryName}}Query
/// </summary>
public class {{QueryName}}QueryHandler : IRequestHandler<{{QueryName}}Query, Result<{{ReturnType}}>>
{
    private readonly I{{EntityName}}Repository _repository;
    private readonly ILogger<{{QueryName}}QueryHandler> _logger;

    public {{QueryName}}QueryHandler(
        I{{EntityName}}Repository repository,
        ILogger<{{QueryName}}QueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<{{ReturnType}}>> Handle(
        {{QueryName}}Query request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Implement query logic here
            // Example for GetById:
            // var result = await _repository.GetByIdAsync(request.Id, cancellationToken);
            // if (!result.IsSuccess)
            //     return Result<{{EntityName}}Dto>.Failure(result.Error);
            // 
            // var dto = MapToDto(result.Value);
            // return Result<{{EntityName}}Dto>.Success(dto);

            // Example for List:
            // var result = await _repository.GetPagedAsync(
            //     request.PageNumber,
            //     request.PageSize,
            //     request.FilterProperty,
            //     cancellationToken);
            // 
            // if (!result.IsSuccess)
            //     return Result<PagedResult<{{EntityName}}Dto>>.Failure(result.Error);
            // 
            // var dtos = result.Value.Items.Select(MapToDto).ToList();
            // var pagedResult = new PagedResult<{{EntityName}}Dto>
            // {
            //     Items = dtos,
            //     TotalCount = result.Value.TotalCount,
            //     PageNumber = request.PageNumber,
            //     PageSize = request.PageSize
            // };
            // 
            // return Result<PagedResult<{{EntityName}}Dto>>.Success(pagedResult);

            throw new NotImplementedException("Implement query logic here");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute {{QueryName}}Query");
            return Result<{{ReturnType}}>.Failure($"Failed to {{QueryDescription}}: {ex.Message}");
        }
    }

    private static {{EntityName}}Dto MapToDto({{EntityName}} entity)
    {
        return new {{EntityName}}Dto
        {
            Id = entity.Id,
            // Map other properties here
        };
    }
}
