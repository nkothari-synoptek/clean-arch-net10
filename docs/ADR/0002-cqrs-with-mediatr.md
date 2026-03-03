# ADR 0002: CQRS with MediatR

## Status

Accepted

## Context

In our Clean Architecture implementation, the Application layer needs a pattern for handling use cases. We need to:

1. Clearly separate commands (write operations) from queries (read operations)
2. Keep controllers thin and focused on HTTP concerns
3. Enable cross-cutting concerns like validation, logging, and transaction management
4. Make use cases explicit and testable
5. Support both synchronous and asynchronous operations

Traditional approaches like service classes can lead to:
- Fat services with many responsibilities
- Unclear boundaries between different use cases
- Difficulty in applying cross-cutting concerns consistently
- Controllers that know too much about business logic

## Decision

We will implement the CQRS (Command Query Responsibility Segregation) pattern using MediatR in the Application layer.

### Structure

**Commands**: Represent actions that change state
```csharp
public record CreateInspectionCommand(string Title, string Description) : IRequest<Result<Guid>>;
```

**Queries**: Represent data retrieval requests
```csharp
public record GetInspectionByIdQuery(Guid Id) : IRequest<Result<InspectionDto>>;
```

**Handlers**: Process commands and queries
```csharp
public class CreateInspectionCommandHandler : IRequestHandler<CreateInspectionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateInspectionCommand request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

**Behaviors**: Cross-cutting concerns applied to all requests
```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // Validates all commands before execution
}

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // Logs all requests and responses
}
```

### Controller Usage

Controllers become thin and delegate to MediatR:
```csharp
[HttpPost]
public async Task<IActionResult> Create(
    [FromBody] CreateInspectionCommand command,
    CancellationToken cancellationToken)
{
    var result = await _mediator.Send(command, cancellationToken);
    
    if (result.IsFailure)
        return BadRequest(result.Error);
    
    return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
}
```

## Consequences

### Positive

1. **Clear Separation**: Commands and queries are explicitly separated
   - Easy to understand what changes state vs. what reads data
   - Can optimize read and write models differently
   - Can apply different validation rules to commands vs. queries

2. **Single Responsibility**: Each handler has one clear purpose
   - Easy to test in isolation
   - Easy to understand and maintain
   - Follows SOLID principles

3. **Thin Controllers**: Controllers delegate to MediatR
   - Controllers focus on HTTP concerns (routing, status codes)
   - Business logic stays in handlers
   - Easy to test controllers

4. **Cross-Cutting Concerns**: Behaviors apply to all requests
   - Validation happens automatically for all commands
   - Logging is consistent across all handlers
   - Transaction management can be centralized
   - No need to remember to add these concerns to each handler

5. **Testability**: Handlers are easy to test
   - Mock dependencies (repositories, services)
   - Test business logic without HTTP concerns
   - Test behaviors independently

6. **Discoverability**: Use cases are explicit
   - Easy to find all commands and queries
   - Clear what the application can do
   - Good for documentation and API design

### Negative

1. **Learning Curve**: Developers need to understand CQRS and MediatR
   - Mitigation: Provide examples and documentation
   - Mitigation: Code reviews to ensure correct usage
   - Mitigation: Templates for common patterns

2. **More Files**: Each use case requires multiple files
   - Command/Query class
   - Handler class
   - Validator class (optional)
   - DTO classes
   - Mitigation: Use scaffolding scripts to generate structure
   - Mitigation: Module-based organization keeps related files together

3. **Indirection**: Extra layer between controller and business logic
   - Mitigation: The benefits of separation outweigh the cost
   - Mitigation: Modern IDEs make navigation easy

4. **Potential Over-Engineering**: May be overkill for very simple CRUD operations
   - Mitigation: Still use CQRS for consistency
   - Mitigation: Simple handlers are still simple

## Alternatives Considered

### 1. Service Layer Pattern

**Structure**: Controllers call service classes with multiple methods

```csharp
public class InspectionService
{
    public Task<Guid> CreateAsync(CreateInspectionDto dto);
    public Task<InspectionDto> GetByIdAsync(Guid id);
    public Task UpdateAsync(Guid id, UpdateInspectionDto dto);
    public Task DeleteAsync(Guid id);
}
```

**Rejected because**:
- Services tend to grow large with many responsibilities
- Difficult to apply cross-cutting concerns consistently
- No clear separation between commands and queries
- Harder to test individual operations in isolation

### 2. Repository Pattern Only

**Structure**: Controllers call repositories directly

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateInspectionDto dto)
{
    var inspection = new Inspection { Title = dto.Title };
    await _repository.AddAsync(inspection);
    return Ok(inspection.Id);
}
```

**Rejected because**:
- Business logic leaks into controllers
- No place for validation, logging, etc.
- Controllers become fat and hard to test
- Violates Single Responsibility Principle

### 3. Direct CQRS (without MediatR)

**Structure**: Implement CQRS pattern manually without a library

**Rejected because**:
- Need to implement request/response pipeline manually
- Need to implement behavior pipeline manually
- MediatR is well-tested and widely used
- Reinventing the wheel

## Implementation Notes

### Registration

```csharp
// Application/DependencyInjection.cs
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
    services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    
    return services;
}
```

### Validation Behavior

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();
        
        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();
        
        if (failures.Any())
            throw new ValidationException(failures);
        
        return await next();
    }
}
```

### Logging Behavior

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        _logger.LogInformation("Handling {RequestName}", requestName);
        
        var response = await next();
        
        _logger.LogInformation("Handled {RequestName}", requestName);
        
        return response;
    }
}
```

### Naming Conventions

- Commands: `{Verb}{Entity}Command` (e.g., `CreateInspectionCommand`, `UpdateInspectionCommand`)
- Queries: `{Verb}{Entity}Query` or `Get{Entity}By{Criteria}Query` (e.g., `GetInspectionByIdQuery`, `ListInspectionsQuery`)
- Handlers: `{Command/Query}Handler` (e.g., `CreateInspectionCommandHandler`)
- Validators: `{Command/Query}Validator` (e.g., `CreateInspectionCommandValidator`)

### Folder Structure

```
Application/
├── Inspections/
│   ├── Commands/
│   │   ├── CreateInspection/
│   │   │   ├── CreateInspectionCommand.cs
│   │   │   ├── CreateInspectionCommandHandler.cs
│   │   │   └── CreateInspectionCommandValidator.cs
│   │   └── UpdateInspection/
│   │       ├── UpdateInspectionCommand.cs
│   │       ├── UpdateInspectionCommandHandler.cs
│   │       └── UpdateInspectionCommandValidator.cs
│   └── Queries/
│       ├── GetInspectionById/
│       │   ├── GetInspectionByIdQuery.cs
│       │   └── GetInspectionByIdQueryHandler.cs
│       └── ListInspections/
│           ├── ListInspectionsQuery.cs
│           └── ListInspectionsQueryHandler.cs
└── Common/
    └── Behaviors/
        ├── ValidationBehavior.cs
        └── LoggingBehavior.cs
```

## Testing Strategy

### Unit Test Handlers

```csharp
public class CreateInspectionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateInspection()
    {
        // Arrange
        var repository = Substitute.For<IInspectionRepository>();
        var handler = new CreateInspectionCommandHandler(repository, logger);
        var command = new CreateInspectionCommand("Test Inspection", "Description");
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).AddAsync(Arg.Any<Inspection>(), Arg.Any<CancellationToken>());
    }
}
```

### Property Test MediatR Pipeline

```csharp
[Property]
public Property MediatR_Should_Process_All_Commands_And_Queries()
{
    return Prop.ForAll(
        Arb.From<IRequest<Result<Guid>>>(),
        async request =>
        {
            var result = await _mediator.Send(request);
            return result != null;
        });
}
```

## References

- [CQRS Pattern by Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)

## Related ADRs

- [ADR 0001: Clean Architecture](0001-clean-architecture.md)
- [ADR 0004: Module-Based Organization](0004-module-based-organization.md)
