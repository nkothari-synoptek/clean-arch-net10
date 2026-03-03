# Microservice Scaffolding Scripts

This directory contains scripts and templates for creating new microservices following the Digital Inspection System architecture.

## Quick Start

### Create a New Microservice

```powershell
.\scripts\New-Microservice.ps1 -ServiceName "Reporting" -ModuleName "Reports"
```

This command will:
1. Create 5 main projects (Domain, Application, Infrastructure, Api, Shared.Kernel)
2. Create 5 test projects (Domain.Tests, Application.Tests, Infrastructure.Tests, Api.Tests, ArchitectureTests)
3. Set up all project references following Clean Architecture dependency rules
4. Add Common.Shared NuGet package references
5. Add all necessary NuGet packages (MediatR, FluentValidation, EF Core, etc.)
6. Create module-based folder structure

### Parameters

- **ServiceName** (required): The name of the microservice (e.g., "Reporting", "MasterData")
  - Must start with an uppercase letter
  - Will be used to create projects like `ReportingService.Domain`
  
- **ModuleName** (required): The name of the initial module/feature (e.g., "Reports", "Products")
  - Must start with an uppercase letter
  - Creates the initial folder structure in each layer

### Examples

```powershell
# Create a Reporting microservice with Reports module
.\scripts\New-Microservice.ps1 -ServiceName "Reporting" -ModuleName "Reports"

# Create a MasterData microservice with Products module
.\scripts\New-Microservice.ps1 -ServiceName "MasterData" -ModuleName "Products"

# Create an Analytics microservice with Dashboards module
.\scripts\New-Microservice.ps1 -ServiceName "Analytics" -ModuleName "Dashboards"
```

## Project Structure Created

After running the script, you'll have the following structure:

```
src/
├── {ServiceName}.Domain/
│   └── {ModuleName}/
│       ├── Entities/
│       ├── ValueObjects/
│       ├── Events/
│       └── Services/
├── {ServiceName}.Application/
│   ├── {ModuleName}/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── DTOs/
│   │   └── Interfaces/
│   └── Common/
│       └── Behaviors/
├── {ServiceName}.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/
│   │   ├── Repositories/{ModuleName}/
│   │   └── Migrations/
│   └── Messaging/
├── {ServiceName}.Api/
│   ├── Controllers/{ModuleName}/
│   └── Middleware/
└── {ServiceName}.Shared.Kernel/
    ├── Base/
    └── Common/

tests/
├── {ServiceName}.Domain.Tests/
├── {ServiceName}.Application.Tests/
├── {ServiceName}.Infrastructure.Tests/
├── {ServiceName}.Api.Tests/
└── {ServiceName}.ArchitectureTests/
```

## Using Templates

After scaffolding, use the templates in the `templates/` directory to create your components:

### Available Templates

1. **Entity.template.cs** - Domain entity with factory methods
2. **CommandHandler.template.cs** - CQRS command handler with validation
3. **QueryHandler.template.cs** - CQRS query handler
4. **Repository.template.cs** - Repository with caching
5. **Controller.template.cs** - REST API controller with CRUD operations
6. **ApplicationDbContext.template.cs** - EF Core DbContext
7. **EntityConfiguration.template.cs** - EF Core entity configuration
8. **Program.template.cs** - API startup configuration
9. **DependencyInjection.template.cs** - Infrastructure DI registration
10. **ApplicationDependencyInjection.template.cs** - Application DI registration
11. **ExceptionHandlingMiddleware.template.cs** - Global exception handling
12. **ValidationBehavior.template.cs** - MediatR validation pipeline
13. **LoggingBehavior.template.cs** - MediatR logging pipeline
14. **appsettings.template.json** - Application configuration

See `templates/README.md` for detailed instructions on using templates.

## Next Steps After Scaffolding

1. **Copy and customize templates** from `scripts/templates/` to your new projects
2. **Update appsettings.json** with your configuration (connection strings, Azure settings)
3. **Implement domain entities** in `{ServiceName}.Domain/{ModuleName}/Entities`
4. **Implement CQRS handlers** in `{ServiceName}.Application/{ModuleName}`
5. **Implement repositories** in `{ServiceName}.Infrastructure/Persistence/Repositories/{ModuleName}`
6. **Implement controllers** in `{ServiceName}.Api/Controllers/{ModuleName}`
7. **Write tests** for each layer
8. **Create database migrations** using EF Core tools
9. **Configure Kubernetes manifests** for deployment

## Architecture Overview

The scaffolding follows Clean Architecture principles:

### Dependency Rules

```
Api → Infrastructure → Application → Domain
                    ↓
              Common.Shared (NuGet)
```

- **Domain**: Zero external dependencies, pure business logic
- **Application**: References Domain only, defines interfaces
- **Infrastructure**: References Application, implements interfaces
- **Api**: References all layers, composition root

### Key Patterns

- **CQRS**: Commands and queries separated using MediatR
- **Repository Pattern**: Data access abstraction with caching
- **Result Pattern**: Consistent error handling
- **Factory Methods**: Entity creation with validation
- **Domain Events**: Significant state changes
- **Dependency Injection**: All dependencies injected via constructor

## Package Management

The scaffolding uses Central Package Management (Directory.Packages.props at solution root):

- All package versions defined centrally
- Projects reference packages without version numbers
- Consistent versions across all projects
- Transitive dependencies pinned

## Common Packages Added

### Application Layer
- MediatR
- FluentValidation
- FluentValidation.DependencyInjectionExtensions

### Infrastructure Layer
- Microsoft.EntityFrameworkCore
- Npgsql.EntityFrameworkCore.PostgreSQL
- Microsoft.EntityFrameworkCore.Design

### API Layer
- Microsoft.AspNetCore.OpenApi
- Swashbuckle.AspNetCore
- Serilog.AspNetCore
- Microsoft.Identity.Web
- AspNetCore.HealthChecks.*

### Test Projects
- xunit
- FluentAssertions
- NSubstitute
- FsCheck
- FsCheck.Xunit
- NetArchTest.Rules (Architecture tests)
- Testcontainers (Infrastructure tests)
- Microsoft.AspNetCore.Mvc.Testing (API tests)

## Troubleshooting

### Script Execution Policy

If you get an execution policy error, run:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### .NET SDK Version

Ensure you have .NET 10.0 SDK installed:

```bash
dotnet --version
```

### Common.Shared Package Not Found

Make sure the Common.Shared package is:
1. Built and published to your private NuGet feed
2. Or available as a project reference in your solution

### Project Already Exists

The script will fail if projects already exist. Either:
1. Choose a different service name
2. Delete existing projects
3. Manually merge if needed

## Best Practices

1. **One microservice per bounded context** - Don't create too many small services
2. **Start with one module** - Add more modules as needed
3. **Follow naming conventions** - Use PascalCase for all names
4. **Keep domain pure** - No infrastructure dependencies in Domain layer
5. **Test each layer** - Use appropriate testing strategies per layer
6. **Use Common.Shared** - Leverage shared infrastructure abstractions
7. **Document decisions** - Add comments explaining complex business rules

## Additional Resources

- **Design Document**: `.kiro/specs/digital-inspection-system/design.md`
- **Requirements Document**: `.kiro/specs/digital-inspection-system/requirements.md`
- **Tasks Document**: `.kiro/specs/digital-inspection-system/tasks.md`
- **Template Documentation**: `scripts/templates/README.md`
- **Example Implementation**: Review the `InspectionService` projects

## Support

For questions or issues:
1. Review the design document for architecture guidance
2. Check the InspectionService implementation for examples
3. Consult the template README for usage instructions
4. Review the requirements document for business context
