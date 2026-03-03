# Architecture Documentation

## Overview

The Digital Inspection System is built using .NET 10 microservices architecture following Clean Architecture principles. This document describes the architectural structure, dependency rules, and organizational patterns used throughout the system.

## Clean Architecture Structure

### Core Principles

1. **Dependency Rule**: Dependencies point inward. Inner layers have no knowledge of outer layers.
2. **Interface Segregation**: Contracts are defined in inner layers and implemented in outer layers.
3. **Single Responsibility**: Each layer has one clear purpose.
4. **Testability**: Each layer can be tested independently with appropriate strategies.
5. **Replaceability**: Infrastructure concerns can be swapped without affecting business logic.

### Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────┐
│                         Api Layer                            │
│  - Program.cs (Composition Root)                             │
│  - Controllers (Thin, delegate to MediatR)                   │
│  - Middleware Configuration                                  │
└────────────────────────┬────────────────────────────────────┘
                         │ References
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                       │
│  - EF Core DbContext & Repositories                          │
│  - Redis Cache Implementation                                │
│  - HTTP Client Adapters                                      │
│  - Azure Service Bus Adapters                                │
└────────────────────────┬────────────────────────────────────┘
                         │ References
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                          │
│  - MediatR Commands & Queries                                │
│  - Command & Query Handlers                                  │
│  - Interface Definitions (IRepository, ICache, etc.)         │
│  - DTOs and Mapping                                          │
└────────────────────────┬────────────────────────────────────┘
                         │ References
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                     Domain Layer                             │
│  - Entities                                                  │
│  - Value Objects                                             │
│  - Domain Services                                           │
│  - Domain Events                                             │
│  - Business Rules                                            │
└─────────────────────────────────────────────────────────────┘
```

#### Domain Layer

**Purpose**: Contains pure business logic with zero external dependencies.

**Allowed Dependencies**:
- None (except optional Shared.Kernel and Common.Shared)
- Pure C# only

**Components**:
- **Entities**: Core business objects with identity (e.g., `Inspection`, `InspectionItem`)
- **Value Objects**: Immutable objects defined by their attributes (e.g., `InspectionStatus`)
- **Domain Services**: Business logic that doesn't belong to a single entity
- **Domain Events**: Events representing domain occurrences
- **Specifications**: Business rule encapsulation for querying

**Example**:
```csharp
// Domain/Inspections/Entities/Inspection.cs
public class Inspection : Entity
{
    public string Title { get; private set; }
    public InspectionStatus Status { get; private set; }
    
    public static Result<Inspection> Create(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<Inspection>.Failure("Title is required");
            
        return Result<Inspection>.Success(new Inspection { Title = title });
    }
    
    public Result<Unit> Complete()
    {
        if (Status == InspectionStatus.Completed)
            return Result<Unit>.Failure("Inspection is already completed");
            
        Status = InspectionStatus.Completed;
        AddDomainEvent(new InspectionCompletedEvent(Id));
        return Result<Unit>.Success(Unit.Value);
    }
}
```

#### Application Layer

**Purpose**: Orchestrates use cases and defines contracts for external concerns.

**Allowed Dependencies**:
- Domain layer
- Common.Shared

**Components**:
- **Commands**: Actions that change state (e.g., `CreateInspectionCommand`)
- **Queries**: Data retrieval requests (e.g., `GetInspectionByIdQuery`)
- **Handlers**: Process commands and queries using MediatR
- **Interfaces**: Contracts for repositories, caching, external services
- **DTOs**: Data transfer objects for API responses
- **Validators**: FluentValidation rules

**Example**:
```csharp
// Application/Inspections/Commands/CreateInspection/CreateInspectionCommandHandler.cs
public class CreateInspectionCommandHandler : IRequestHandler<CreateInspectionCommand, Result<Guid>>
{
    private readonly IInspectionRepository _repository;
    private readonly ILogger<CreateInspectionCommandHandler> _logger;
    
    public async Task<Result<Guid>> Handle(
        CreateInspectionCommand request,
        CancellationToken cancellationToken)
    {
        var inspection = Inspection.Create(request.Title);
        if (inspection.IsFailure)
            return Result<Guid>.Failure(inspection.Error);
            
        await _repository.AddAsync(inspection.Value, cancellationToken);
        
        _logger.LogInformation("Created inspection {InspectionId}", inspection.Value.Id);
        
        return Result<Guid>.Success(inspection.Value.Id);
    }
}
```

#### Infrastructure Layer

**Purpose**: Implements all external concerns and adapters.

**Allowed Dependencies**:
- Application layer (and transitively Domain)
- Common.Shared
- External packages (EF Core, StackExchange.Redis, etc.)

**Components**:
- **Persistence**: EF Core DbContext, entity configurations, repositories
- **Caching**: Cache implementations using Common.Shared abstractions
- **External Services**: HTTP client adapters for external APIs
- **Messaging**: Message publishers and consumers using Common.Shared
- **File Storage**: Azure Blob Storage adapters

**Example**:
```csharp
// Infrastructure/Persistence/Repositories/Inspections/InspectionRepository.cs
public class InspectionRepository : IInspectionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCacheService _cache;
    
    public async Task<Result<Inspection>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var cacheKey = $"inspection:{id}";
        
        // Check cache first
        var cached = await _cache.GetAsync<Inspection>(cacheKey, cancellationToken);
        if (cached != null)
            return Result<Inspection>.Success(cached);
        
        // Retrieve from database
        var inspection = await _context.Inspections
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        
        if (inspection == null)
            return Result<Inspection>.Failure($"Inspection with ID {id} not found");
        
        // Cache for 15 minutes
        await _cache.SetAsync(cacheKey, inspection, TimeSpan.FromMinutes(15), cancellationToken);
        
        return Result<Inspection>.Success(inspection);
    }
}
```

#### API Layer

**Purpose**: Web host and composition root.

**Allowed Dependencies**:
- All other layers (Domain, Application, Infrastructure)
- Common.Shared
- ASP.NET Core packages

**Components**:
- **Program.cs**: Application entry point, DI configuration
- **Controllers**: Thin controllers that delegate to MediatR
- **Middleware**: Custom middleware for logging, error handling
- **Configuration**: appsettings.json, environment-specific settings

**Example**:
```csharp
// Api/Controllers/Inspections/InspectionsController.cs
[ApiController]
[Route("api/[controller]")]
public class InspectionsController : ControllerBase
{
    private readonly IMediator _mediator;
    
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
}
```

## Dependency Rules

### Rule 1: Dependencies Point Inward

Inner layers must not reference outer layers. The dependency flow is:

```
Api → Infrastructure → Application → Domain
```

**Enforcement**: Architecture tests using NetArchTest verify these rules automatically.

### Rule 2: Domain Has Zero External Dependencies

The Domain layer must not reference any external packages except:
- Shared.Kernel (optional)
- Common.Shared (for base classes only)

**Enforcement**: Architecture tests fail if Domain references Infrastructure or Application.

### Rule 3: Application Defines Interfaces

The Application layer defines interfaces for external concerns (repositories, caching, messaging). The Infrastructure layer implements these interfaces.

**Example**:
```csharp
// Application/Inspections/Interfaces/IInspectionRepository.cs
public interface IInspectionRepository
{
    Task<Result<Inspection>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Inspection inspection, CancellationToken cancellationToken);
}

// Infrastructure/Persistence/Repositories/Inspections/InspectionRepository.cs
public class InspectionRepository : IInspectionRepository
{
    // Implementation
}
```

### Rule 4: API Layer is the Composition Root

All dependency injection registration happens in the API layer's Program.cs. Each layer provides extension methods for registering its services.

**Example**:
```csharp
// Api/Program.cs
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

## Module-Based Organization

### Structure

Code is organized by module (feature) rather than by technical concern. Each module contains all related code across layers.

```
Domain/
├── Inspections/                    # Inspection module
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Services/
│   └── Events/
└── Inspectors/                     # Inspector module
    ├── Entities/
    └── ValueObjects/

Application/
├── Inspections/                    # Inspection module
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   └── Interfaces/
└── Inspectors/                     # Inspector module
    ├── Commands/
    └── Queries/

Infrastructure/
├── Persistence/
│   ├── Configurations/
│   │   ├── InspectionConfiguration.cs
│   │   └── InspectorConfiguration.cs
│   └── Repositories/
│       ├── Inspections/
│       └── Inspectors/
└── Messaging/
    ├── InspectionEventPublisher.cs
    └── InspectorEventPublisher.cs

Api/
└── Controllers/
    ├── Inspections/
    └── Inspectors/
```

### Benefits

1. **Cohesion**: Related code is located together
2. **Discoverability**: Easy to find all code for a feature
3. **Scalability**: New features are added as new modules
4. **Team Organization**: Teams can own specific modules

### Adding a New Module

1. Create module folder in Domain layer with entities and value objects
2. Create module folder in Application layer with commands, queries, and interfaces
3. Create repository implementations in Infrastructure layer
4. Create controller in API layer
5. Register services in DependencyInjection.cs

## Common.Shared Library

### Purpose

Common.Shared is a private NuGet package containing reusable components shared across all microservices.

### Components

- **ServiceBus**: Generic Azure Service Bus abstractions and implementations
- **Caching**: Generic Redis caching abstractions and implementations
- **Logging**: High-performance logging delegates using LoggerMessage.Define
- **Observability**: OpenTelemetry configuration and helpers
- **Authentication**: Service-to-service authentication handlers
- **Resilience**: Polly HTTP resilience policies

### Usage

Microservices reference Common.Shared for infrastructure abstractions but implement microservice-specific logic in their Infrastructure layer.

**Example**:
```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Register Common.Shared services
    services.AddCommonSharedServices(configuration);
    
    // Register microservice-specific repositories
    services.AddScoped<IInspectionRepository, InspectionRepository>();
    
    return services;
}
```

## System Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     External Clients                         │
│                  (Web, Mobile, Desktop)                      │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTPS
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway (YARP)                        │
│  - Azure Entra External Authentication                       │
│  - Authorization                                             │
│  - Rate Limiting                                             │
│  - Request Routing                                           │
└─────┬───────────────┬───────────────┬───────────────────────┘
      │               │               │
      │ Internal      │ Internal      │ Internal
      │ (No Auth)     │ (No Auth)     │ (No Auth)
      ▼               ▼               ▼
┌──────────┐    ┌──────────┐    ┌──────────┐
│Inspection│    │ Reporting│    │  Master  │
│ Service  │    │ Service  │    │   Data   │
│          │    │          │    │ Service  │
└────┬─────┘    └────┬─────┘    └────┬─────┘
     │               │               │
     └───────┬───────┴───────┬───────┘
             │               │
             ▼               ▼
      ┌──────────┐    ┌──────────┐
      │  Redis   │    │  Azure   │
      │  Cache   │    │ Service  │
      │          │    │   Bus    │
      └──────────┘    └──────────┘
```

### API Gateway

- **Technology**: YARP (Yet Another Reverse Proxy)
- **Authentication**: Azure Entra External (JWT tokens)
- **Authorization**: Policy-based authorization
- **Responsibilities**: 
  - Authenticate all external requests
  - Route requests to appropriate microservices
  - Rate limiting
  - Request logging

### Microservices

Each microservice:
- Follows Clean Architecture with seven projects
- Uses CQRS pattern with MediatR
- Implements caching with Redis
- Publishes events to Azure Service Bus
- Exposes REST APIs
- Deploys independently to AKS

### Communication Patterns

1. **Synchronous**: REST APIs for request-response
2. **Asynchronous**: Azure Service Bus for events
3. **Caching**: Redis for frequently accessed data

## Testing Strategy

### By Layer

| Layer | Test Type | Dependencies | Framework |
|-------|-----------|--------------|-----------|
| Domain | Unit Tests | None | xUnit |
| Application | Unit Tests | Mocked repositories | xUnit + NSubstitute |
| Infrastructure | Integration Tests | Real database (Testcontainers) | xUnit + Testcontainers |
| API | Integration Tests | WebApplicationFactory | xUnit |
| Architecture | Architecture Tests | NetArchTest | xUnit + NetArchTest |

### Test Projects

Each microservice has five test projects:
- `{Service}.Domain.Tests`
- `{Service}.Application.Tests`
- `{Service}.Infrastructure.Tests`
- `{Service}.Api.Tests`
- `{Service}.ArchitectureTests`

## Deployment

### Azure Kubernetes Service (AKS)

Each microservice deploys to AKS with:
- **Deployment**: Defines pods, containers, resource limits
- **Service**: Exposes pods internally (ClusterIP) or externally (LoadBalancer for gateway)
- **HorizontalPodAutoscaler**: Scales based on CPU/memory metrics
- **Ingress**: Routes external traffic to API Gateway

### Observability

- **Tracing**: OpenTelemetry with Azure Monitor
- **Metrics**: Custom metrics with OpenTelemetry
- **Logging**: Serilog with structured logging
- **Health Checks**: Liveness and readiness probes

## References

- [Developer Onboarding Guide](DEVELOPER_ONBOARDING.md)
- [Architecture Decision Records](ADR/)
- [Architectural Compliance Checklist](COMPLIANCE_CHECKLIST.md)
