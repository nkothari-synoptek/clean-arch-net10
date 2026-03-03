# Microservice Templates

This directory contains template files for creating new microservices following the Digital Inspection System architecture.

## Available Templates

### Domain Layer Templates

- **Entity.template.cs**: Template for creating domain entities with factory methods and domain logic
- **EntityConfiguration.template.cs**: Template for EF Core entity configuration using Fluent API

### Application Layer Templates

- **CommandHandler.template.cs**: Template for CQRS command handlers with validation
- **QueryHandler.template.cs**: Template for CQRS query handlers
- **ApplicationDependencyInjection.template.cs**: Template for Application layer DI registration
- **ValidationBehavior.template.cs**: Template for MediatR validation pipeline behavior
- **LoggingBehavior.template.cs**: Template for MediatR logging pipeline behavior

### Infrastructure Layer Templates

- **Repository.template.cs**: Template for repository implementation with caching
- **ApplicationDbContext.template.cs**: Template for EF Core DbContext
- **DependencyInjection.template.cs**: Template for Infrastructure layer DI registration

### API Layer Templates

- **Controller.template.cs**: Template for REST API controllers with CRUD operations
- **Program.template.cs**: Template for API startup configuration
- **ExceptionHandlingMiddleware.template.cs**: Template for global exception handling middleware
- **appsettings.template.json**: Template for application configuration

## Using Templates

### 1. Create a New Microservice

Use the PowerShell script to scaffold a new microservice:

```powershell
.\scripts\New-Microservice.ps1 -ServiceName "Reporting" -ModuleName "Reports"
```

This creates the complete project structure with all necessary projects and references.

### 2. Copy and Customize Templates

After scaffolding, copy the relevant templates to your new projects and replace the placeholders:

#### Placeholders to Replace

- `{{ServiceName}}` - The service name (e.g., "ReportingService")
- `{{ModuleName}}` - The module/feature name (e.g., "Reports")
- `{{EntityName}}` - The entity name (e.g., "Report")
- `{{EntityNamePlural}}` - The plural entity name (e.g., "Reports")
- `{{entityName}}` - The lowercase entity name (e.g., "report")
- `{{serviceName}}` - The lowercase service name (e.g., "reportingservice")
- `{{CommandName}}` - The command name (e.g., "CreateReport")
- `{{QueryName}}` - The query name (e.g., "GetReportById")
- `{{CommandDescription}}` - Description of what the command does
- `{{QueryDescription}}` - Description of what the query does
- `{{ReturnType}}` - The return type (e.g., "Guid", "ReportDto", "Unit")

### 3. Example: Creating a New Entity

1. Copy `Entity.template.cs` to your Domain project:
   ```
   src/ReportingService.Domain/Reports/Entities/Report.cs
   ```

2. Replace placeholders:
   - `{{ServiceName}}` → `ReportingService`
   - `{{ModuleName}}` → `Reports`
   - `{{EntityName}}` → `Report`

3. Add your specific properties and domain logic

### 4. Example: Creating a Command Handler

1. Copy `CommandHandler.template.cs` to your Application project:
   ```
   src/ReportingService.Application/Reports/Commands/CreateReport/CreateReportCommandHandler.cs
   ```

2. Replace placeholders:
   - `{{ServiceName}}` → `ReportingService`
   - `{{ModuleName}}` → `Reports`
   - `{{CommandName}}` → `CreateReport`
   - `{{CommandDescription}}` → `create a new report`
   - `{{ReturnType}}` → `Guid`
   - `{{EntityName}}` → `Report`

3. Implement the command logic

### 5. Example: Creating a Repository

1. Copy `Repository.template.cs` to your Infrastructure project:
   ```
   src/ReportingService.Infrastructure/Persistence/Repositories/Reports/ReportRepository.cs
   ```

2. Replace placeholders:
   - `{{ServiceName}}` → `ReportingService`
   - `{{ModuleName}}` → `Reports`
   - `{{EntityName}}` → `Report`
   - `{{EntityNamePlural}}` → `Reports`
   - `{{entityName}}` → `report`

3. Customize caching and query logic as needed

### 6. Example: Creating a Controller

1. Copy `Controller.template.cs` to your API project:
   ```
   src/ReportingService.Api/Controllers/Reports/ReportsController.cs
   ```

2. Replace placeholders:
   - `{{ServiceName}}` → `ReportingService`
   - `{{ModuleName}}` → `Reports`
   - `{{EntityName}}` → `Report`
   - `{{EntityNamePlural}}` → `Reports`

3. Adjust authorization policies as needed

## Template Replacement Script

You can use a simple PowerShell script to replace placeholders:

```powershell
$content = Get-Content "template.cs" -Raw
$content = $content -replace '{{ServiceName}}', 'ReportingService'
$content = $content -replace '{{ModuleName}}', 'Reports'
$content = $content -replace '{{EntityName}}', 'Report'
$content | Set-Content "output.cs"
```

## Best Practices

1. **Always start with the scaffolding script** - It creates the correct project structure and references
2. **Customize templates for your needs** - These are starting points, not rigid requirements
3. **Follow naming conventions** - Use PascalCase for entities, commands, and queries
4. **Keep domain logic in entities** - Don't put business rules in handlers or repositories
5. **Use Result pattern** - Return Result<T> from handlers and repositories for consistent error handling
6. **Add proper logging** - Use structured logging with meaningful context
7. **Write tests** - Create corresponding test files for each component

## Architecture Guidelines

### Domain Layer
- Zero external dependencies (except Shared.Kernel)
- Pure business logic
- Factory methods for entity creation
- Domain events for significant state changes

### Application Layer
- References Domain only
- CQRS with MediatR
- FluentValidation for input validation
- Interface definitions for infrastructure concerns

### Infrastructure Layer
- References Application (and transitively Domain)
- Implements interfaces from Application
- Uses Common.Shared for infrastructure abstractions
- EF Core for data access
- Repository pattern with caching

### API Layer
- References all layers
- Thin controllers that delegate to MediatR
- Global exception handling
- Authentication and authorization
- Health checks and observability

## Additional Resources

- See the main design document at `.kiro/specs/digital-inspection-system/design.md`
- See the requirements document at `.kiro/specs/digital-inspection-system/requirements.md`
- Review the InspectionService implementation for complete examples
