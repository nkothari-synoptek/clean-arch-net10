# Quick Reference Guide

## One-Command Scaffolding

```powershell
.\scripts\New-Microservice.ps1 -ServiceName "YourService" -ModuleName "YourModule"
```

## Template Files Quick Reference

| Template | Purpose | Location After Copy |
|----------|---------|---------------------|
| Entity.template.cs | Domain entity | `Domain/{Module}/Entities/{Entity}.cs` |
| CommandHandler.template.cs | CQRS command | `Application/{Module}/Commands/{Command}/{Command}Handler.cs` |
| QueryHandler.template.cs | CQRS query | `Application/{Module}/Queries/{Query}/{Query}Handler.cs` |
| Repository.template.cs | Data access | `Infrastructure/Persistence/Repositories/{Module}/{Entity}Repository.cs` |
| Controller.template.cs | REST API | `Api/Controllers/{Module}/{Entities}Controller.cs` |
| ApplicationDbContext.template.cs | EF Core context | `Infrastructure/Persistence/ApplicationDbContext.cs` |
| EntityConfiguration.template.cs | EF Core config | `Infrastructure/Persistence/Configurations/{Entity}Configuration.cs` |
| Program.template.cs | API startup | `Api/Program.cs` |
| DependencyInjection.template.cs | Infrastructure DI | `Infrastructure/DependencyInjection.cs` |
| ApplicationDependencyInjection.template.cs | Application DI | `Application/DependencyInjection.cs` |
| ExceptionHandlingMiddleware.template.cs | Error handling | `Api/Middleware/ExceptionHandlingMiddleware.cs` |
| ValidationBehavior.template.cs | MediatR validation | `Application/Common/Behaviors/ValidationBehavior.cs` |
| LoggingBehavior.template.cs | MediatR logging | `Application/Common/Behaviors/LoggingBehavior.cs` |
| appsettings.template.json | Configuration | `Api/appsettings.json` |

## Placeholder Replacement Cheat Sheet

```
{{ServiceName}}        → ReportingService
{{serviceName}}        → reportingservice
{{ModuleName}}         → Reports
{{EntityName}}         → Report
{{EntityNamePlural}}   → Reports
{{entityName}}         → report
{{CommandName}}        → CreateReport
{{QueryName}}          → GetReportById
{{CommandDescription}} → create a new report
{{QueryDescription}}   → retrieve a report by ID
{{ReturnType}}         → Guid | Unit | ReportDto
```

## Project Structure After Scaffolding

```
src/
├── {Service}.Domain/
│   └── {Module}/
│       ├── Entities/
│       ├── ValueObjects/
│       ├── Events/
│       └── Services/
├── {Service}.Application/
│   ├── {Module}/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── DTOs/
│   │   └── Interfaces/
│   └── Common/
│       └── Behaviors/
├── {Service}.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/
│   │   ├── Repositories/{Module}/
│   │   └── Migrations/
│   └── Messaging/
├── {Service}.Api/
│   ├── Controllers/{Module}/
│   └── Middleware/
└── {Service}.Shared.Kernel/
    ├── Base/
    └── Common/

tests/
├── {Service}.Domain.Tests/
├── {Service}.Application.Tests/
├── {Service}.Infrastructure.Tests/
├── {Service}.Api.Tests/
└── {Service}.ArchitectureTests/
```

## Common Commands

### Build and Test
```bash
# Build solution
dotnet build

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/ReportingService.Domain.Tests
```

### Database Migrations
```bash
# Create migration
cd src/{Service}.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../{Service}.Api

# Update database
dotnet ef database update --startup-project ../{Service}.Api

# Remove last migration
dotnet ef migrations remove --startup-project ../{Service}.Api
```

### Run Services
```bash
# Run API
cd src/{Service}.Api
dotnet run

# Run with specific environment
dotnet run --environment Development
```

## Dependency Rules

```
✓ Api → Infrastructure → Application → Domain
✓ Application → Common.Shared
✓ Infrastructure → Common.Shared
✓ Api → Common.Shared

✗ Domain → Application
✗ Domain → Infrastructure
✗ Domain → Api
✗ Application → Infrastructure
✗ Application → Api
```

## Package References by Layer

### Domain
- None (except optional Shared.Kernel)

### Application
- MediatR
- FluentValidation
- FluentValidation.DependencyInjectionExtensions
- Common.Shared

### Infrastructure
- Microsoft.EntityFrameworkCore
- Npgsql.EntityFrameworkCore.PostgreSQL
- Microsoft.EntityFrameworkCore.Design
- Common.Shared

### API
- Microsoft.AspNetCore.OpenApi
- Swashbuckle.AspNetCore
- Serilog.AspNetCore
- Microsoft.Identity.Web
- AspNetCore.HealthChecks.*
- Common.Shared

### All Test Projects
- xunit
- FluentAssertions
- NSubstitute
- FsCheck
- FsCheck.Xunit

### Specific Test Projects
- NetArchTest.Rules (Architecture tests)
- Testcontainers, Testcontainers.PostgreSql (Infrastructure tests)
- Microsoft.AspNetCore.Mvc.Testing (API tests)

## Common Patterns

### Entity Creation
```csharp
public static Entity Create(params)
{
    // Validation
    Guard.Against.NullOrEmpty(name, nameof(name));
    
    var entity = new Entity
    {
        Id = Guid.NewGuid(),
        // Properties
    };
    
    entity.AddDomainEvent(new EntityCreatedEvent(entity.Id));
    return entity;
}
```

### Command Handler
```csharp
public async Task<Result<T>> Handle(Command request, CancellationToken ct)
{
    try
    {
        // Business logic
        var entity = Entity.Create(request.Param);
        await _repository.AddAsync(entity, ct);
        
        _logger.LogInformation("Created {Entity}", entity.Id);
        return Result<T>.Success(entity.Id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create {Entity}");
        return Result<T>.Failure(ex.Message);
    }
}
```

### Query Handler with Caching
```csharp
public async Task<Result<Dto>> Handle(Query request, CancellationToken ct)
{
    var cacheKey = $"entity:{request.Id}";
    
    // Try cache
    var cached = await _cache.GetAsync<Dto>(cacheKey, ct);
    if (cached != null)
        return Result<Dto>.Success(cached);
    
    // Fetch from repository
    var result = await _repository.GetByIdAsync(request.Id, ct);
    if (!result.IsSuccess)
        return Result<Dto>.Failure(result.Error);
    
    var dto = MapToDto(result.Value);
    await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(15), ct);
    
    return Result<Dto>.Success(dto);
}
```

### Repository with Cache Invalidation
```csharp
public async Task<Result<Unit>> UpdateAsync(Entity entity, CancellationToken ct)
{
    _context.Entities.Update(entity);
    await _context.SaveChangesAsync(ct);
    
    // Invalidate cache
    await _cache.RemoveAsync($"entity:{entity.Id}", ct);
    
    return Result<Unit>.Success(Unit.Value);
}
```

### Controller Action
```csharp
[HttpPost]
[Authorize(Policy = "CanCreate")]
[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Create([FromBody] CreateCommand command)
{
    var result = await _mediator.Send(command);
    
    if (!result.IsSuccess)
        return BadRequest(new { error = result.Error });
    
    return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
}
```

## Health Check Endpoints

```
GET /health          - Overall health
GET /health/ready    - Readiness (DB, cache)
GET /health/live     - Liveness (always returns 200)
```

## Swagger/OpenAPI

```
Development: https://localhost:5001/swagger
```

## Common Issues

### Issue: Common.Shared package not found
**Solution**: Build and publish Common.Shared to your NuGet feed first

### Issue: Migration fails
**Solution**: Ensure connection string is correct in appsettings.json

### Issue: Tests fail with "Cannot access disposed object"
**Solution**: Ensure DbContext is properly scoped in tests

### Issue: Circular dependency
**Solution**: Check dependency rules - likely Infrastructure referencing Api

### Issue: Authentication fails
**Solution**: Verify Azure Entra ID configuration in appsettings.json

## Best Practices Checklist

- [ ] Domain layer has zero external dependencies
- [ ] All commands have validators
- [ ] All handlers use Result<T> pattern
- [ ] All repositories implement caching
- [ ] All controllers have authorization policies
- [ ] All exceptions are logged
- [ ] All public APIs have XML documentation
- [ ] All layers have corresponding tests
- [ ] Architecture tests verify dependency rules
- [ ] Health checks are configured
- [ ] OpenTelemetry is configured
- [ ] Serilog is configured

## Resources

- **Full Documentation**: `scripts/README.md`
- **Usage Example**: `scripts/USAGE_EXAMPLE.md`
- **Template Guide**: `scripts/templates/README.md`
- **Design Document**: `.kiro/specs/digital-inspection-system/design.md`
- **Requirements**: `.kiro/specs/digital-inspection-system/requirements.md`
