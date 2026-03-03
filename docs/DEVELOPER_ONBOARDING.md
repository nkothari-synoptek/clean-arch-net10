# Developer Onboarding Guide

Welcome to the Digital Inspection System! This guide will help you get started with development, understand the codebase structure, and learn how to add new features.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Getting Started](#getting-started)
3. [Project Structure](#project-structure)
4. [Creating a New Microservice](#creating-a-new-microservice)
5. [Adding a New Feature](#adding-a-new-feature)
6. [Running Tests](#running-tests)
7. [Deployment](#deployment)
8. [Common Tasks](#common-tasks)
9. [Best Practices](#best-practices)

## Prerequisites

### Required Tools

- **.NET 10 SDK**: [Download](https://dotnet.microsoft.com/download)
- **Docker Desktop**: For running Redis, PostgreSQL, and Testcontainers
- **PowerShell 7+**: For running scaffolding scripts
- **Visual Studio 2025** or **Rider 2025** (recommended IDEs)
- **Azure CLI**: For AKS deployment
- **kubectl**: For Kubernetes management

### Optional Tools

- **k9s**: Terminal UI for Kubernetes
- **Postman** or **Insomnia**: For API testing
- **Azure Data Studio**: For database management

### Knowledge Requirements

- C# and .NET fundamentals
- Clean Architecture principles
- CQRS pattern
- Entity Framework Core
- Docker and Kubernetes basics

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd DigitalInspectionSystem
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Start Infrastructure Services

```bash
# Start Redis and PostgreSQL using Docker Compose
docker-compose up -d
```

### 4. Run Database Migrations

```bash
cd src/InspectionService.Api
dotnet ef database update --project ../InspectionService.Infrastructure
```

### 5. Run the Application

```bash
# Terminal 1: Start Inspection Service
cd src/InspectionService.Api
dotnet run

# Terminal 2: Start API Gateway
cd src/ApiGateway
dotnet run
```

### 6. Verify Setup

Navigate to:
- API Gateway: `https://localhost:5001/swagger`
- Inspection Service: `https://localhost:5101/swagger`

## Project Structure

### Solution Organization

```
DigitalInspectionSystem/
├── src/
│   ├── ApiGateway/                      # YARP API Gateway
│   ├── Common.Shared/                   # Shared NuGet package
│   ├── InspectionService.Domain/        # Business logic
│   ├── InspectionService.Application/   # Use cases
│   ├── InspectionService.Infrastructure/# External concerns
│   ├── InspectionService.Api/          # Web host
│   └── InspectionService.Shared.Kernel/# Cross-cutting primitives
├── tests/
│   ├── InspectionService.Domain.Tests/
│   ├── InspectionService.Application.Tests/
│   ├── InspectionService.Infrastructure.Tests/
│   ├── InspectionService.Api.Tests/
│   └── InspectionService.ArchitectureTests/
├── k8s/                                # Kubernetes manifests
├── scripts/                            # Scaffolding scripts
├── docs/                               # Documentation
├── Directory.Packages.props            # Central package management
└── DigitalInspectionSystem.sln
```

### Microservice Structure

Each microservice follows this seven-project structure:

```
{ServiceName}/
├── {ServiceName}.Domain/           # Entities, value objects, domain logic
├── {ServiceName}.Application/      # Commands, queries, handlers
├── {ServiceName}.Infrastructure/   # Repositories, adapters, DbContext
├── {ServiceName}.Api/             # Controllers, middleware, Program.cs
├── {ServiceName}.Shared.Kernel/   # Shared primitives (optional)
├── {ServiceName}.Domain.Tests/
├── {ServiceName}.Application.Tests/
├── {ServiceName}.Infrastructure.Tests/
├── {ServiceName}.Api.Tests/
└── {ServiceName}.ArchitectureTests/
```

## Creating a New Microservice

### Using the PowerShell Script

The fastest way to create a new microservice is using the provided scaffolding script:

```powershell
cd scripts
./New-Microservice.ps1 -ServiceName "Reporting"
```

This creates:
- Seven projects with correct structure
- Project references
- Common.Shared NuGet reference
- Module-based folder structure
- Template files for common patterns

### Manual Creation Steps

If you prefer manual creation:

1. **Create Domain Project**
```bash
dotnet new classlib -n ReportingService.Domain
```

2. **Create Application Project**
```bash
dotnet new classlib -n ReportingService.Application
dotnet add ReportingService.Application reference ReportingService.Domain
```

3. **Create Infrastructure Project**
```bash
dotnet new classlib -n ReportingService.Infrastructure
dotnet add ReportingService.Infrastructure reference ReportingService.Application
```

4. **Create API Project**
```bash
dotnet new webapi -n ReportingService.Api
dotnet add ReportingService.Api reference ReportingService.Domain
dotnet add ReportingService.Api reference ReportingService.Application
dotnet add ReportingService.Api reference ReportingService.Infrastructure
```

5. **Create Test Projects**
```bash
dotnet new xunit -n ReportingService.Domain.Tests
dotnet new xunit -n ReportingService.Application.Tests
dotnet new xunit -n ReportingService.Infrastructure.Tests
dotnet new xunit -n ReportingService.Api.Tests
dotnet new xunit -n ReportingService.ArchitectureTests
```

6. **Add to Solution**
```bash
dotnet sln add src/ReportingService.Domain
dotnet sln add src/ReportingService.Application
dotnet sln add src/ReportingService.Infrastructure
dotnet sln add src/ReportingService.Api
dotnet sln add tests/ReportingService.Domain.Tests
dotnet sln add tests/ReportingService.Application.Tests
dotnet sln add tests/ReportingService.Infrastructure.Tests
dotnet sln add tests/ReportingService.Api.Tests
dotnet sln add tests/ReportingService.ArchitectureTests
```

## Adding a New Feature

### Example: Adding a "Reports" Feature to Inspection Service

#### Step 1: Create Domain Entity

```csharp
// Domain/Reports/Entities/Report.cs
namespace InspectionService.Domain.Reports.Entities;

public class Report : Entity
{
    public string Title { get; private set; }
    public string Content { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    
    private Report() { } // EF Core
    
    public static Result<Report> Create(string title, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<Report>.Failure("Title is required");
        
        if (string.IsNullOrWhiteSpace(content))
            return Result<Report>.Failure("Content is required");
        
        return Result<Report>.Success(new Report
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            GeneratedAt = DateTime.UtcNow
        });
    }
}
```

#### Step 2: Create Application Command

```csharp
// Application/Reports/Commands/CreateReport/CreateReportCommand.cs
namespace InspectionService.Application.Reports.Commands.CreateReport;

public record CreateReportCommand(string Title, string Content) : IRequest<Result<Guid>>;

// Application/Reports/Commands/CreateReport/CreateReportCommandValidator.cs
public class CreateReportCommandValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);
        
        RuleFor(x => x.Content)
            .NotEmpty();
    }
}

// Application/Reports/Commands/CreateReport/CreateReportCommandHandler.cs
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
        var report = Report.Create(request.Title, request.Content);
        if (report.IsFailure)
            return Result<Guid>.Failure(report.Error);
        
        await _repository.AddAsync(report.Value, cancellationToken);
        
        _logger.LogInformation("Created report {ReportId}", report.Value.Id);
        
        return Result<Guid>.Success(report.Value.Id);
    }
}
```

#### Step 3: Create Repository Interface

```csharp
// Application/Reports/Interfaces/IReportRepository.cs
namespace InspectionService.Application.Reports.Interfaces;

public interface IReportRepository
{
    Task<Result<Report>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Report report, CancellationToken cancellationToken);
}
```

#### Step 4: Implement Repository

```csharp
// Infrastructure/Persistence/Repositories/Reports/ReportRepository.cs
namespace InspectionService.Infrastructure.Persistence.Repositories.Reports;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCacheService _cache;
    
    public ReportRepository(
        ApplicationDbContext context,
        IDistributedCacheService cache)
    {
        _context = context;
        _cache = cache;
    }
    
    public async Task<Result<Report>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var cacheKey = $"report:{id}";
        
        var cached = await _cache.GetAsync<Report>(cacheKey, cancellationToken);
        if (cached != null)
            return Result<Report>.Success(cached);
        
        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        
        if (report == null)
            return Result<Report>.Failure($"Report with ID {id} not found");
        
        await _cache.SetAsync(cacheKey, report, TimeSpan.FromMinutes(15), cancellationToken);
        
        return Result<Report>.Success(report);
    }
    
    public async Task AddAsync(Report report, CancellationToken cancellationToken)
    {
        await _context.Reports.AddAsync(report, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

#### Step 5: Configure Entity

```csharp
// Infrastructure/Persistence/Configurations/ReportConfiguration.cs
namespace InspectionService.Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");
        
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(r => r.Content)
            .IsRequired();
        
        builder.Property(r => r.GeneratedAt)
            .IsRequired();
    }
}
```

#### Step 6: Add DbSet to DbContext

```csharp
// Infrastructure/Persistence/ApplicationDbContext.cs
public DbSet<Report> Reports => Set<Report>();
```

#### Step 7: Register Repository

```csharp
// Infrastructure/DependencyInjection.cs
services.AddScoped<IReportRepository, ReportRepository>();
```

#### Step 8: Create Controller

```csharp
// Api/Controllers/Reports/ReportsController.cs
namespace InspectionService.Api.Controllers.Reports;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateReportCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.IsFailure)
            return BadRequest(result.Error);
        
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetReportByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        
        if (result.IsFailure)
            return NotFound(result.Error);
        
        return Ok(result.Value);
    }
}
```

#### Step 9: Create Migration

```bash
cd src/InspectionService.Api
dotnet ef migrations add AddReports --project ../InspectionService.Infrastructure
dotnet ef database update --project ../InspectionService.Infrastructure
```

#### Step 10: Write Tests

```csharp
// Domain.Tests/Reports/ReportTests.cs
public class ReportTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var title = "Test Report";
        var content = "Test content";
        
        // Act
        var result = Report.Create(title, content);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be(title);
        result.Value.Content.Should().Be(content);
    }
}
```

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Tests for Specific Project

```bash
dotnet test tests/InspectionService.Domain.Tests
```

### Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~InspectionTests.Create_WithValidData_ShouldSucceed"
```

### Run Architecture Tests

```bash
dotnet test tests/InspectionService.ArchitectureTests
```

Architecture tests verify:
- Domain has zero external dependencies
- Application only references Domain
- Infrastructure only references Application
- Dependency flow is unidirectional inward

### Run Integration Tests

Integration tests use Testcontainers for real database instances:

```bash
# Ensure Docker is running
docker ps

# Run integration tests
dotnet test tests/InspectionService.Infrastructure.Tests
```

## Deployment

### Local Development

```bash
# Start infrastructure services
docker-compose up -d

# Run migrations
cd src/InspectionService.Api
dotnet ef database update --project ../InspectionService.Infrastructure

# Run services
dotnet run --project src/InspectionService.Api
dotnet run --project src/ApiGateway
```

### Deploy to AKS

#### Prerequisites

1. Azure subscription
2. AKS cluster created
3. Azure Container Registry (ACR)
4. kubectl configured

#### Build and Push Images

```bash
# Build Inspection Service image
docker build -t myacr.azurecr.io/inspection-service:latest -f src/InspectionService.Api/Dockerfile .
docker push myacr.azurecr.io/inspection-service:latest

# Build API Gateway image
docker build -t myacr.azurecr.io/api-gateway:latest -f src/ApiGateway/Dockerfile .
docker push myacr.azurecr.io/api-gateway:latest
```

#### Deploy to Kubernetes

```bash
cd k8s

# Create namespace
kubectl apply -f namespace.yaml

# Deploy Inspection Service
kubectl apply -f inspection-service-configmap.yaml
kubectl apply -f inspection-service-secrets.yaml
kubectl apply -f inspection-service-deployment.yaml
kubectl apply -f inspection-service-service.yaml
kubectl apply -f inspection-service-hpa.yaml

# Deploy API Gateway
kubectl apply -f api-gateway-configmap.yaml
kubectl apply -f api-gateway-secrets.yaml
kubectl apply -f api-gateway-deployment.yaml
kubectl apply -f api-gateway-service.yaml
kubectl apply -f api-gateway-ingress.yaml
```

#### Verify Deployment

```bash
# Check pods
kubectl get pods -n digital-inspection

# Check services
kubectl get services -n digital-inspection

# Check logs
kubectl logs -f deployment/inspection-service -n digital-inspection
```

#### Quick Deploy Script

```bash
cd k8s
./deploy.sh
```

#### Cleanup

```bash
cd k8s
./cleanup.sh
```

## Common Tasks

### Add a New NuGet Package

1. Add package version to `Directory.Packages.props`:
```xml
<PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
```

2. Add package reference to project (without version):
```xml
<PackageReference Include="Newtonsoft.Json" />
```

### Update Common.Shared Package

1. Make changes to `src/Common.Shared`
2. Increment version in `Common.Shared.csproj`
3. Build and pack:
```bash
cd src/Common.Shared
dotnet pack -c Release
```
4. Publish to private NuGet feed (Azure Artifacts)

### Add a New Domain Event

```csharp
// Domain/Reports/Events/ReportGeneratedEvent.cs
public record ReportGeneratedEvent(Guid ReportId, DateTime GeneratedAt) : IDomainEvent;
```

### Publish Domain Event

```csharp
// Infrastructure/Messaging/ReportEventPublisher.cs
public class ReportEventPublisher
{
    private readonly IMessagePublisher _publisher;
    
    public async Task PublishReportGeneratedAsync(
        ReportGeneratedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var message = new ServiceBusMessage
        {
            Subject = "ReportGenerated",
            Body = JsonSerializer.Serialize(domainEvent)
        };
        
        await _publisher.PublishAsync("report-events", message, cancellationToken);
    }
}
```

### Add Health Check

```csharp
// Api/Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis"))
    .AddAzureServiceBusTopic(
        builder.Configuration.GetConnectionString("ServiceBus"),
        "inspection-events");

app.MapHealthChecks("/health");
```

## Best Practices

### Code Organization

1. **Follow module-based structure**: Group related code by feature, not by technical concern
2. **Keep controllers thin**: Delegate to MediatR handlers
3. **Use Result pattern**: Return `Result<T>` instead of throwing exceptions for business logic failures
4. **Validate at boundaries**: Use FluentValidation in Application layer

### Domain Layer

1. **No external dependencies**: Domain should be pure C#
2. **Use factory methods**: Create entities with static factory methods
3. **Encapsulate business rules**: Keep business logic in entities and domain services
4. **Raise domain events**: Use domain events for side effects

### Application Layer

1. **One handler per command/query**: Keep handlers focused
2. **Define interfaces**: Let Infrastructure implement them
3. **Use DTOs**: Don't expose domain entities in API responses
4. **Validate commands**: Use FluentValidation

### Infrastructure Layer

1. **Implement interfaces**: Implement contracts defined in Application
2. **Use Common.Shared**: Leverage shared abstractions for caching, messaging, etc.
3. **Configure entities**: Use Fluent API for EF Core configurations
4. **Handle errors**: Log and handle infrastructure failures gracefully

### Testing

1. **Test each layer independently**: Use appropriate test strategies
2. **Use Testcontainers**: For integration tests requiring real databases
3. **Mock external dependencies**: Use NSubstitute for unit tests
4. **Run architecture tests**: Verify dependency rules automatically

### Performance

1. **Use caching**: Cache frequently accessed data with Redis
2. **Use async/await**: All I/O operations should be async
3. **Use LoggerMessage.Define**: For high-performance logging
4. **Optimize queries**: Use EF Core query optimization techniques

### Security

1. **Validate input**: Always validate user input
2. **Use parameterized queries**: EF Core does this by default
3. **Implement authorization**: Use policy-based authorization
4. **Secure secrets**: Use Azure Key Vault or Kubernetes secrets

## Getting Help

- **Architecture Questions**: See [ARCHITECTURE.md](ARCHITECTURE.md)
- **Compliance Checklist**: See [COMPLIANCE_CHECKLIST.md](COMPLIANCE_CHECKLIST.md)
- **Architecture Decisions**: See [ADR/](ADR/)
- **Team Chat**: [Your team chat link]
- **Issue Tracker**: [Your issue tracker link]

## Next Steps

1. Read the [Architecture Documentation](ARCHITECTURE.md)
2. Review the [Architectural Compliance Checklist](COMPLIANCE_CHECKLIST.md)
3. Explore the [Architecture Decision Records](ADR/)
4. Try creating a new feature following the guide above
5. Run the test suite and verify everything passes

Welcome aboard! 🚀
