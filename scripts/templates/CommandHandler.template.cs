using Common.Shared.Logging;
using MediatR;
using Microsoft.Extensions.Logging;
using {{ServiceName}}.Application.{{ModuleName}}.Interfaces;
using {{ServiceName}}.Shared.Kernel.Common;

namespace {{ServiceName}}.Application.{{ModuleName}}.Commands.{{CommandName}};

/// <summary>
/// Command to {{CommandDescription}}
/// </summary>
public record {{CommandName}}Command : IRequest<Result<{{ReturnType}}>>
{
    // Add command properties here
    // Example:
    // public Guid Id { get; init; }
    // public string Name { get; init; }
}

/// <summary>
/// Validator for {{CommandName}}Command
/// </summary>
public class {{CommandName}}CommandValidator : AbstractValidator<{{CommandName}}Command>
{
    public {{CommandName}}CommandValidator()
    {
        // Add validation rules here
        // Example:
        // RuleFor(x => x.Name)
        //     .NotEmpty().WithMessage("Name is required")
        //     .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
    }
}

/// <summary>
/// Handler for {{CommandName}}Command
/// </summary>
public class {{CommandName}}CommandHandler : IRequestHandler<{{CommandName}}Command, Result<{{ReturnType}}>>
{
    private readonly I{{EntityName}}Repository _repository;
    private readonly ILogger<{{CommandName}}CommandHandler> _logger;

    public {{CommandName}}CommandHandler(
        I{{EntityName}}Repository repository,
        ILogger<{{CommandName}}CommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<{{ReturnType}}>> Handle(
        {{CommandName}}Command request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Implement command logic here
            // Example for Create:
            // var entity = {{EntityName}}.Create(request.Name, request.Description);
            // await _repository.AddAsync(entity, cancellationToken);
            // _logger.LogInformation("Created {{EntityName}} with ID {EntityId}", entity.Id);
            // return Result<Guid>.Success(entity.Id);

            // Example for Update:
            // var result = await _repository.GetByIdAsync(request.Id, cancellationToken);
            // if (!result.IsSuccess)
            //     return Result<Unit>.Failure(result.Error);
            // 
            // var entity = result.Value;
            // entity.Update(request.Name, request.Description);
            // await _repository.UpdateAsync(entity, cancellationToken);
            // _logger.LogInformation("Updated {{EntityName}} with ID {EntityId}", entity.Id);
            // return Result<Unit>.Success(Unit.Value);

            // Example for Delete:
            // var result = await _repository.GetByIdAsync(request.Id, cancellationToken);
            // if (!result.IsSuccess)
            //     return Result<Unit>.Failure(result.Error);
            // 
            // await _repository.DeleteAsync(request.Id, cancellationToken);
            // _logger.LogInformation("Deleted {{EntityName}} with ID {EntityId}", request.Id);
            // return Result<Unit>.Success(Unit.Value);

            throw new NotImplementedException("Implement command logic here");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute {{CommandName}}Command");
            return Result<{{ReturnType}}>.Failure($"Failed to {{CommandDescription}}: {ex.Message}");
        }
    }
}
