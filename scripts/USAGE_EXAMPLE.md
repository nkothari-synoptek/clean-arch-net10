# Scaffolding Script Usage Example

This document provides a complete walkthrough of creating a new microservice using the scaffolding tools.

## Scenario: Creating a Reporting Microservice

Let's create a new Reporting microservice with a Reports module that manages inspection reports.

### Step 1: Run the Scaffolding Script

```powershell
# Navigate to the solution root
cd /path/to/DigitalInspectionSystem

# Run the scaffolding script
.\scripts\New-Microservice.ps1 -ServiceName "Reporting" -ModuleName "Reports"
```

**Output:**
```
Creating microservice: ReportingService
Initial module: Reports

Creating project directories...
Creating main projects...
  Creating ReportingService.Domain...
  Creating ReportingService.Application...
  Creating ReportingService.Infrastructure...
  Creating ReportingService.Api...
  Creating ReportingService.Shared.Kernel...

Creating test projects...
  Creating ReportingService.Domain.Tests...
  Creating ReportingService.Application.Tests...
  Creating ReportingService.Infrastructure.Tests...
  Creating ReportingService.Api.Tests...
  Creating ReportingService.ArchitectureTests...

Adding project references...
  Application -> Domain
  Infrastructure -> Application
  Api -> Domain, Application, Infrastructure
  Test projects -> Main projects

Adding Common.Shared package references...
Adding common NuGet packages...
  Application layer packages...
  Infrastructure layer packages...
  API layer packages...
  Test packages...

Creating module-based folder structure...
  Created Domain/Reports structure
  Created Application/Reports structure
  Created Application/Common structure
  Created Infrastructure/Persistence structure
  Created Infrastructure/Messaging structure
  Created Api/Controllers/Reports structure
  Created Api/Middleware structure
  Created Shared.Kernel structure

Microservice scaffolding completed successfully!
```

### Step 2: Project Structure Created

```
src/
├── ReportingService.Domain/
│   └── Reports/
│       ├── Entities/
│       ├── ValueObjects/
│       ├── Events/
│       └── Services/
├── ReportingService.Application/
│   ├── Reports/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── DTOs/
│   │   └── Interfaces/
│   └── Common/
│       └── Behaviors/
├── ReportingService.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/
│   │   ├── Repositories/Reports/
│   │   └── Migrations/
│   └── Messaging/
├── ReportingService.Api/
│   ├── Controllers/Reports/
│   └── Middleware/
└── ReportingService.Shared.Kernel/
    ├── Base/
    └── Common/

tests/
├── ReportingService.Domain.Tests/
├── ReportingService.Application.Tests/
├── ReportingService.Infrastructure.Tests/
├── ReportingService.Api.Tests/
└── ReportingService.ArchitectureTests/
```

### Step 3: Create Domain Entity

Copy the entity template and customize it:

```bash
# Copy template
cp scripts/templates/Entity.template.cs src/ReportingService.Domain/Reports/Entities/Report.cs
```

Replace placeholders in `Report.cs`:

```csharp
using ReportingService.Shared.Kernel.Base;

namespace ReportingService.Domain.Reports.Entities;

public class Report : Entity
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Content { get; private set; }
    public Guid InspectionId { get; private set; }
    public DateTime GeneratedDate { get; private set; }

    private Report() { }

    public static Report Create(string title, string content, Guid inspectionId)
    {
        var report = new Report
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            InspectionId = inspectionId,
            GeneratedDate = DateTime.UtcNow
        };

        return report;
    }
}
```

### Step 4: Create Repository Interface

In `src/ReportingService.Application/Reports/Interfaces/IReportRepository.cs`:

```csharp
using ReportingService.Domain.Reports.Entities;
using ReportingService.Shared.Kernel.Common;

namespace ReportingService.Application.Reports.Interfaces;

public interface IReportRepository
{
    Task<Result<Report>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Unit>> AddAsync(Report report, CancellationToken cancellationToken = default);
}
```

### Step 5: Create Command Handler

Copy and customize the command handler template:

```bash
cp scripts/templates/CommandHandler.template.cs src/ReportingService.Application/Reports/Commands/CreateReport/CreateReportCommandHandler.cs
```

After customization:

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using ReportingService.Application.Reports.Interfaces;
using ReportingService.Domain.Reports.Entities;
using ReportingService.Shared.Kernel.Common;

namespace ReportingService.Application.Reports.Commands.CreateReport;

public record CreateReportCommand : IRequest<Result<Guid>>
{
    public string Title { get; init; }
    public string Content { get; init; }
    public Guid InspectionId { get; init; }
}

public class CreateReportCommandHandler : IRequestHandler<CreateReportCommand, Result<Guid>>
{
    private readonly IReportRepository _repository;
    private readonly ILogger<CreateReportCommandHandler> _logger;

    public CreateReportCommandHandler(
        IReportRepository repository,
        ILogger<CreateReportCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(
        CreateReportCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = Report.Create(request.Title, request.Content, request.InspectionId);
            await _repository.AddAsync(report, cancellationToken);
            
            _logger.LogInformation("Created report with ID {ReportId}", report.Id);
            return Result<Guid>.Success(report.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create report");
            return Result<Guid>.Failure($"Failed to create report: {ex.Message}");
        }
    }
}
```

### Step 6: Create Repository Implementation

Copy and customize the repository template:

```bash
cp scripts/templates/Repository.template.cs src/ReportingService.Infrastructure/Persistence/Repositories/Reports/ReportRepository.cs
```

### Step 7: Create Controller

Copy and customize the controller template:

```bash
cp scripts/templates/Controller.template.cs src/ReportingService.Api/Controllers/Reports/ReportsController.cs
```

### Step 8: Configure Infrastructure

Copy and customize the DependencyInjection template:

```bash
cp scripts/templates/DependencyInjection.template.cs src/ReportingService.Infrastructure/DependencyInjection.cs
```

Update to register your repository:

```csharp
// Register repositories
services.AddScoped<IReportRepository, ReportRepository>();
```

### Step 9: Configure Application

Copy and customize the Application DependencyInjection:

```bash
cp scripts/templates/ApplicationDependencyInjection.template.cs src/ReportingService.Application/DependencyInjection.cs
```

### Step 10: Configure API

Copy and customize Program.cs:

```bash
cp scripts/templates/Program.template.cs src/ReportingService.Api/Program.cs
```

Copy and customize appsettings.json:

```bash
cp scripts/templates/appsettings.template.json src/ReportingService.Api/appsettings.json
```

### Step 11: Create DbContext

Copy and customize the DbContext template:

```bash
cp scripts/templates/ApplicationDbContext.template.cs src/ReportingService.Infrastructure/Persistence/ApplicationDbContext.cs
```

Update with your DbSet:

```csharp
public DbSet<Report> Reports => Set<Report>();
```

### Step 12: Create Entity Configuration

Copy and customize the entity configuration:

```bash
cp scripts/templates/EntityConfiguration.template.cs src/ReportingService.Infrastructure/Persistence/Configurations/ReportConfiguration.cs
```

### Step 13: Create Middleware

Copy the exception handling middleware:

```bash
cp scripts/templates/ExceptionHandlingMiddleware.template.cs src/ReportingService.Api/Middleware/ExceptionHandlingMiddleware.cs
```

### Step 14: Create Behaviors

Copy the validation and logging behaviors:

```bash
cp scripts/templates/ValidationBehavior.template.cs src/ReportingService.Application/Common/Behaviors/ValidationBehavior.cs
cp scripts/templates/LoggingBehavior.template.cs src/ReportingService.Application/Common/Behaviors/LoggingBehavior.cs
```

### Step 15: Build and Test

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Create initial migration
cd src/ReportingService.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../ReportingService.Api

# Run the API
cd ../ReportingService.Api
dotnet run
```

## Quick Reference: Placeholder Replacements

When copying templates, replace these placeholders:

| Placeholder | Example Value |
|-------------|---------------|
| `{{ServiceName}}` | `ReportingService` |
| `{{serviceName}}` | `reportingservice` |
| `{{ModuleName}}` | `Reports` |
| `{{EntityName}}` | `Report` |
| `{{EntityNamePlural}}` | `Reports` |
| `{{entityName}}` | `report` |
| `{{CommandName}}` | `CreateReport` |
| `{{QueryName}}` | `GetReportById` |
| `{{CommandDescription}}` | `create a new report` |
| `{{QueryDescription}}` | `retrieve a report by ID` |
| `{{ReturnType}}` | `Guid`, `ReportDto`, `Unit` |

## Automated Replacement Script

You can create a PowerShell script to automate placeholder replacement:

```powershell
# Replace-Placeholders.ps1
param(
    [string]$TemplateFile,
    [string]$OutputFile,
    [hashtable]$Replacements
)

$content = Get-Content $TemplateFile -Raw

foreach ($key in $Replacements.Keys) {
    $content = $content -replace $key, $Replacements[$key]
}

$content | Set-Content $OutputFile
```

Usage:

```powershell
.\Replace-Placeholders.ps1 `
    -TemplateFile "scripts/templates/Entity.template.cs" `
    -OutputFile "src/ReportingService.Domain/Reports/Entities/Report.cs" `
    -Replacements @{
        '{{ServiceName}}' = 'ReportingService'
        '{{ModuleName}}' = 'Reports'
        '{{EntityName}}' = 'Report'
    }
```

## Tips

1. **Start with the domain** - Define your entities and business logic first
2. **Work outward** - Then create application handlers, infrastructure repositories, and finally API controllers
3. **Test as you go** - Write tests for each layer as you implement it
4. **Use the InspectionService as reference** - It's a complete working example
5. **Follow the dependency rules** - Domain has no dependencies, Application references Domain only, etc.
6. **Leverage Common.Shared** - Use the shared infrastructure abstractions for caching, messaging, etc.

## Next Steps

After creating your microservice:

1. Add it to the solution file
2. Create Kubernetes manifests (use the k8s/ directory as reference)
3. Configure the API Gateway to route to your new service
4. Add health checks and observability
5. Write comprehensive tests
6. Document your API with Swagger
7. Deploy to your environment
