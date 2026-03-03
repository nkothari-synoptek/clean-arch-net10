# Design Document: Digital Inspection System

## Overview

The digital inspection system is built using .NET 10 microservices architecture following Clean Architecture principles. Each microservice is structured with five distinct projects that enforce strict dependency rules, ensuring maintainability, testability, and scalability. The system uses YARP as an API gateway with Azure Entra External for authentication, and deploys to Azure Kubernetes Service (AKS).

### Core Design Principles

1. **Dependency Rule**: Dependencies point inward. Inner layers know nothing about outer layers.
2. **Interface Segregation**: Define contracts in inner layers, implement in outer layers.
3. **Single Responsibility**: Each layer has one clear purpose.
4. **Testability**: Each layer can be tested independently with appropriate strategies.
5. **Replaceability**: Infrastructure concerns can be swapped without affecting business logic.

## Architecture

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

### Microservice Internal Architecture

Each microservice follows a seven-project structure with test projects:

```
MyService/
├── src/
│   ├── MyService.Domain/           # Business logic, entities, domain services
│   ├── MyService.Application/      # Use cases, CQRS handlers, interfaces
│   ├── MyService.Infrastructure/   # EF Core, Redis, HTTP clients, adapters
│   ├── MyService.Api/             # Web host, DI configuration, controllers
│   └── MyService.Shared.Kernel/   # Cross-cutting primitives (optional)
├── tests/
│   ├── MyService.Domain.Tests/    # Domain unit tests
│   ├── MyService.Application.Tests/ # Application unit tests
│   ├── MyService.Infrastructure.Tests/ # Infrastructure integration tests
│   ├── MyService.Api.Tests/       # API integration tests
│   └── MyService.ArchitectureTests/ # Architecture compliance tests
└── Common.Shared/                 # Private NuGet package for shared components
    ├── ServiceBus/                # Azure Service Bus abstractions
    ├── Caching/                   # Redis caching abstractions
    ├── Logging/                   # High-performance logging delegates
    ├── Observability/             # OpenTelemetry configuration
    └── Authentication/            # Service-to-service auth helpers
```

### Dependency Flow

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

## Components and Interfaces

### 1. Domain Layer

**Purpose**: Contains pure business logic with zero external dependencies.

**Components**:
- **Entities**: Core business objects with identity (e.g., `Inspection`, `InspectionItem`)
- **Value Objects**: Immutable objects defined by their attributes (e.g., `InspectionStatus`, `Address`)
- **Domain Services**: Business logic that doesn't belong to a single entity
- **Domain Events**: Events that represent something that happened in the domain
- **Specifications**: Business rule encapsulation for querying

**Key Characteristics**:
- No dependencies on other projects (except optional Shared.Kernel)
- No infrastructure concerns (no database, no HTTP, no file I/O)
- Pure C# with business logic only

**Example Structure**:
```
Domain/
├── Inspections/                    # Inspection module
│   ├── Entities/
│   │   ├── Inspection.cs
│   │   └── InspectionItem.cs
│   ├── ValueObjects/
│   │   └── InspectionStatus.cs
│   ├── Services/
│   │   └── InspectionDomainService.cs
│   ├── Events/
│   │   └── InspectionCompletedEvent.cs
│   └── Specifications/
│       └── ActiveInspectionSpecification.cs
├── Inspectors/                     # Inspector module
│   ├── Entities/
│   │   └── Inspector.cs
│   └── ValueObjects/
│       └── InspectorCertification.cs
└── Common/                         # Shared domain primitives
    ├── Entity.cs
    └── ValueObject.cs
```

### 2. Application Layer

**Purpose**: Orchestrates use cases and defines contracts for external concerns.

**Components**:
- **Commands**: Represent actions that change state (e.g., `CreateInspectionCommand`)
- **Queries**: Represent data retrieval requests (e.g., `GetInspectionByIdQuery`)
- **Handlers**: Process commands and queries using MediatR
- **Interfaces**: Contracts for repositories, caching, external services
- **DTOs**: Data transfer objects for API responses
- **Validators**: FluentValidation rules for commands and queries

**Key Characteristics**:
- References Domain layer only
- Defines interfaces, Infrastructure implements them
- Uses MediatR for CQRS pattern
- No knowledge of databases, HTTP, or other infrastructure

**Example Structure**:
```
Application/
├── Inspections/                    # Inspection module
│   ├── Commands/
│   │   ├── CreateInspection/
│   │   │   ├── CreateInspectionCommand.cs
│   │   │   ├── CreateInspectionCommandHandler.cs
│   │   │   └── CreateInspectionCommandValidator.cs
│   │   ├── UpdateInspection/
│   │   │   ├── UpdateInspectionCommand.cs
│   │   │   ├── UpdateInspectionCommandHandler.cs
│   │   │   └── UpdateInspectionCommandValidator.cs
│   │   └── DeleteInspection/
│   │       ├── DeleteInspectionCommand.cs
│   │       └── DeleteInspectionCommandHandler.cs
│   ├── Queries/
│   │   ├── GetInspectionById/
│   │   │   ├── GetInspectionByIdQuery.cs
│   │   │   └── GetInspectionByIdQueryHandler.cs
│   │   └── ListInspections/
│   │       ├── ListInspectionsQuery.cs
│   │       └── ListInspectionsQueryHandler.cs
│   ├── DTOs/
│   │   ├── InspectionDto.cs
│   │   └── InspectionItemDto.cs
│   └── Interfaces/
│       └── IInspectionRepository.cs
├── Inspectors/                     # Inspector module
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   └── Interfaces/
└── Common/
    ├── Interfaces/
    │   ├── ICacheService.cs
    │   └── INotificationService.cs
    ├── Behaviors/
    │   ├── ValidationBehavior.cs
    │   └── LoggingBehavior.cs
    └── Mappings/
```

### 3. Infrastructure Layer

**Purpose**: Implements all external concerns and adapters specific to this microservice.

**Components**:
- **Persistence**: EF Core DbContext, entity configurations, repository implementations (microservice-specific)
- **Caching**: Microservice-specific cache implementations using Common.Shared abstractions
- **External Services**: HTTP client adapters for external APIs (microservice-specific integrations)
- **Messaging**: Microservice-specific message publishers and consumers using Common.Shared abstractions
- **File Storage**: Azure Blob Storage adapters (if needed)

**Key Characteristics**:
- References Application layer (and transitively Domain)
- Implements interfaces defined in Application layer
- Uses Common.Shared for infrastructure abstractions (Redis, Service Bus, etc.)
- Contains microservice-specific infrastructure code only

**What Goes in Infrastructure vs Common.Shared**:

**Infrastructure Layer** (Microservice-Specific):
- Repository implementations for this microservice's entities
- DbContext and entity configurations for this microservice's database
- Adapters for external APIs specific to this microservice
- Message handlers for this microservice's business events
- Microservice-specific caching strategies

**Common.Shared** (Reusable Across All Microservices):
- Generic Redis cache service (IDistributedCacheService, RedisCacheService)
- Generic Service Bus publisher/consumer (IMessagePublisher, IMessageConsumer)
- Generic HTTP client resilience policies
- OpenTelemetry configuration
- High-performance logging delegates
- Service-to-service authentication handlers

**Example Structure**:
```
Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs              # Microservice-specific
│   ├── Configurations/
│   │   ├── InspectionConfiguration.cs       # Microservice-specific
│   │   └── InspectorConfiguration.cs        # Microservice-specific
│   ├── Repositories/
│   │   ├── Inspections/
│   │   │   └── InspectionRepository.cs      # Uses Common.Shared.IDistributedCacheService
│   │   └── Inspectors/
│   │       └── InspectorRepository.cs       # Uses Common.Shared.IDistributedCacheService
│   └── Migrations/
├── ExternalServices/
│   ├── NotificationServiceAdapter.cs        # Microservice-specific, uses Common.Shared HTTP policies
│   └── MasterDataServiceAdapter.cs          # Microservice-specific, uses Common.Shared HTTP policies
├── Messaging/
│   ├── InspectionEventPublisher.cs          # Uses Common.Shared.IMessagePublisher
│   └── MasterDataEventConsumer.cs           # Uses Common.Shared.IMessageConsumer
└── DependencyInjection.cs
```

**Example: Repository Using Common.Shared Cache**:
```csharp
// Infrastructure/Persistence/Repositories/Inspections/InspectionRepository.cs
public class InspectionRepository : IInspectionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCacheService _cache;  // From Common.Shared
    private readonly ILogger<InspectionRepository> _logger;
    
    public InspectionRepository(
        ApplicationDbContext context,
        IDistributedCacheService cache,  // Injected from Common.Shared
        ILogger<InspectionRepository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<Result<Inspection>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var cacheKey = $"inspection:{id}";
        
        // Use Common.Shared cache abstraction
        var cached = await _cache.GetAsync<Inspection>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogInformation("Retrieved inspection {InspectionId} from cache", id);
            return Result<Inspection>.Success(cached);
        }
        
        var inspection = await _context.Inspections
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        
        if (inspection == null)
            return Result<Inspection>.Failure($"Inspection with ID {id} not found");
        
        // Cache for 15 minutes using Common.Shared
        await _cache.SetAsync(cacheKey, inspection, TimeSpan.FromMinutes(15), cancellationToken);
        
        return Result<Inspection>.Success(inspection);
    }
}
```

**Example: External Service Adapter Using Common.Shared**:
```csharp
// Infrastructure/ExternalServices/NotificationServiceAdapter.cs
public class NotificationServiceAdapter : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationServiceAdapter> _logger;
    
    // HttpClient is configured with Common.Shared resilience policies in DI
    public NotificationServiceAdapter(
        HttpClient httpClient,
        ILogger<NotificationServiceAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<Result<Unit>> SendInspectionCompletedNotificationAsync(
        Guid inspectionId,
        string recipientEmail,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new NotificationRequest
            {
                Type = "InspectionCompleted",
                RecipientEmail = recipientEmail,
                Data = new { InspectionId = inspectionId }
            };
            
            // HTTP client already has Polly policies from Common.Shared
            var response = await _httpClient.PostAsJsonAsync(
                "/api/notifications",
                request,
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for inspection {InspectionId}", inspectionId);
            return Result<Unit>.Failure($"Failed to send notification: {ex.Message}");
        }
    }
}
```

**Example: Message Publisher Using Common.Shared**:
```csharp
// Infrastructure/Messaging/InspectionEventPublisher.cs
public class InspectionEventPublisher
{
    private readonly IMessagePublisher _publisher;  // From Common.Shared
    private readonly ILogger<InspectionEventPublisher> _logger;
    
    public InspectionEventPublisher(
        IMessagePublisher publisher,  // Injected from Common.Shared
        ILogger<InspectionEventPublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }
    
    public async Task PublishInspectionCompletedAsync(
        InspectionCompletedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var message = new ServiceBusMessage
        {
            Subject = "InspectionCompleted",
            Body = JsonSerializer.Serialize(domainEvent),
            ApplicationProperties =
            {
                ["InspectionId"] = domainEvent.InspectionId.ToString(),
                ["CompletedAt"] = domainEvent.CompletedAt.ToString("O")
            }
        };
        
        // Use Common.Shared message publisher
        await _publisher.PublishAsync("inspection-events", message, cancellationToken);
        
        _logger.LogInformation(
            "Published InspectionCompleted event for inspection {InspectionId}",
            domainEvent.InspectionId);
    }
}
```

**DependencyInjection.cs Configuration**:
```csharp
// Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Common.Shared services
        services.AddCommonSharedServices(configuration);  // From Common.Shared
        
        // Register DbContext (microservice-specific)
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        
        // Register repositories (microservice-specific)
        services.AddScoped<IInspectionRepository, InspectionRepository>();
        services.AddScoped<IInspectorRepository, InspectorRepository>();
        
        // Register external service adapters with Common.Shared HTTP policies
        services.AddHttpClient<INotificationService, NotificationServiceAdapter>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:NotificationService:BaseUrl"]);
        })
        .AddCommonResiliencePolicies();  // From Common.Shared
        
        services.AddHttpClient<IMasterDataService, MasterDataServiceAdapter>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:MasterDataService:BaseUrl"]);
        })
        .AddCommonResiliencePolicies()   // From Common.Shared
        .AddServiceAuthentication();      // From Common.Shared
        
        // Register message publishers (microservice-specific, uses Common.Shared abstractions)
        services.AddScoped<InspectionEventPublisher>();
        
        return services;
    }
}
```

### 4. API Layer

**Purpose**: Web host and composition root. Thin layer that wires everything together.

**Components**:
- **Program.cs**: Application entry point, DI configuration
- **Controllers**: Thin controllers that delegate to MediatR
- **Middleware**: Custom middleware for logging, error handling
- **Configuration**: appsettings.json, environment-specific settings

**Key Characteristics**:
- References all other layers
- Configures dependency injection
- Minimal business logic
- ASP.NET Core web host

**Example Structure**:
```
Api/
├── Controllers/
│   ├── Inspections/
│   │   └── InspectionsController.cs
│   └── Inspectors/
│       └── InspectorsController.cs
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── Directory.Packages.props          # Central Package Management
```

### 5. Shared Kernel (Optional)

**Purpose**: Cross-cutting primitives shared across layers within a single microservice.

**Components**:
- **Base Classes**: Entity base class, ValueObject base class
- **Common Interfaces**: IEntity, IAuditable
- **Result Pattern**: Result<T> for error handling
- **Guard Clauses**: Input validation helpers

**Example Structure**:
```
Shared.Kernel/
├── Base/
│   ├── Entity.cs
│   └── ValueObject.cs
├── Interfaces/
│   └── IAuditable.cs
└── Common/
    ├── Result.cs
    └── Guard.cs
```

### 6. Common.Shared (Private NuGet Package)

**Purpose**: Shared components distributed as a private NuGet package for use across multiple microservices.

**Components**:
- **ServiceBus**: Azure Service Bus abstractions and implementations
- **Caching**: Redis caching abstractions and implementations
- **Logging**: High-performance logging delegates using LoggerMessage.Define
- **Observability**: OpenTelemetry configuration and helpers
- **Authentication**: Service-to-service authentication handlers
- **Resilience**: Polly HTTP resilience policies

**Example Structure**:
```
Common.Shared/
├── ServiceBus/
│   ├── IMessagePublisher.cs
│   ├── IMessageConsumer.cs
│   ├── ServiceBusPublisher.cs          # Generic implementation
│   ├── ServiceBusConsumer.cs           # Generic implementation
│   └── ServiceBusExtensions.cs
├── Caching/
│   ├── IDistributedCacheService.cs
│   ├── RedisCacheService.cs            # Generic Redis implementation
│   └── CacheExtensions.cs
├── Logging/
│   ├── LogMessages.cs                  # High-performance logging delegates
│   └── LoggingExtensions.cs
├── Observability/
│   ├── OpenTelemetryExtensions.cs
│   ├── ActivitySourceProvider.cs
│   └── MetricsProvider.cs
├── Authentication/
│   ├── ServiceAuthenticationHandler.cs
│   └── ServiceAuthenticationExtensions.cs
└── Resilience/
    ├── HttpResiliencePolicies.cs
    └── ResilienceExtensions.cs
```

**Generic Redis Cache Service Implementation**:
```csharp
// Common.Shared/Caching/IDistributedCacheService.cs
public interface IDistributedCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

// Common.Shared/Caching/RedisCacheService.cs
public class RedisCacheService : IDistributedCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    
    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(key);
        
        if (!value.HasValue)
            return default;
        
        return JsonSerializer.Deserialize<T>(value!);
    }
    
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var serialized = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, serialized, expiration);
    }
    
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
    }
    
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(key);
    }
}

// Common.Shared/Caching/CacheExtensions.cs
public static class CacheExtensions
{
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")));
        
        services.AddSingleton<IDistributedCacheService, RedisCacheService>();
        
        return services;
    }
}
```

**Generic Service Bus Implementation**:
```csharp
// Common.Shared/ServiceBus/IMessagePublisher.cs
public interface IMessagePublisher
{
    Task PublishAsync(string topicName, ServiceBusMessage message, CancellationToken cancellationToken = default);
    Task PublishBatchAsync(string topicName, IEnumerable<ServiceBusMessage> messages, CancellationToken cancellationToken = default);
}

// Common.Shared/ServiceBus/ServiceBusPublisher.cs
public class ServiceBusPublisher : IMessagePublisher
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();
    
    public ServiceBusPublisher(
        ServiceBusClient client,
        ILogger<ServiceBusPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }
    
    public async Task PublishAsync(
        string topicName,
        ServiceBusMessage message,
        CancellationToken cancellationToken = default)
    {
        var sender = _senders.GetOrAdd(topicName, _client.CreateSender(topicName));
        
        try
        {
            await sender.SendMessageAsync(message, cancellationToken);
            _logger.LogInformation("Published message to topic {TopicName}", topicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to topic {TopicName}", topicName);
            throw;
        }
    }
    
    public async Task PublishBatchAsync(
        string topicName,
        IEnumerable<ServiceBusMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var sender = _senders.GetOrAdd(topicName, _client.CreateSender(topicName));
        
        try
        {
            await sender.SendMessagesAsync(messages, cancellationToken);
            _logger.LogInformation("Published batch of messages to topic {TopicName}", topicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch to topic {TopicName}", topicName);
            throw;
        }
    }
}

// Common.Shared/ServiceBus/ServiceBusExtensions.cs
public static class ServiceBusExtensions
{
    public static IServiceCollection AddServiceBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(sp =>
            new ServiceBusClient(
                configuration.GetConnectionString("ServiceBus"),
                new ServiceBusClientOptions
                {
                    TransportType = ServiceBusTransportType.AmqpWebSockets
                }));
        
        services.AddSingleton<IMessagePublisher, ServiceBusPublisher>();
        
        return services;
    }
}
```

**Generic HTTP Resilience Policies**:
```csharp
// Common.Shared/Resilience/HttpResiliencePolicies.cs
public static class HttpResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Retry {RetryCount} after {Delay}ms due to {Reason}",
                        retryCount,
                        timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }
    
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Circuit breaker opened for {Duration}s due to {Reason}",
                        duration.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                },
                onReset: context =>
                {
                    var logger = context.GetLogger();
                    logger?.LogInformation("Circuit breaker reset");
                });
    }
    
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
    }
}

// Common.Shared/Resilience/ResilienceExtensions.cs
public static class ResilienceExtensions
{
    public static IHttpClientBuilder AddCommonResiliencePolicies(this IHttpClientBuilder builder)
    {
        return builder
            .AddPolicyHandler(HttpResiliencePolicies.GetRetryPolicy())
            .AddPolicyHandler(HttpResiliencePolicies.GetCircuitBreakerPolicy())
            .AddPolicyHandler(HttpResiliencePolicies.GetTimeoutPolicy());
    }
}
```

**Common.Shared Registration Extension**:
```csharp
// Common.Shared/CommonSharedExtensions.cs
public static class CommonSharedExtensions
{
    public static IServiceCollection AddCommonSharedServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Redis cache
        services.AddRedisCache(configuration);
        
        // Add Service Bus
        services.AddServiceBus(configuration);
        
        // Add OpenTelemetry
        services.AddObservability(configuration);
        
        // Add service-to-service authentication
        services.AddServiceToServiceAuthentication(configuration);
        
        return services;
    }
}
```

**Summary of Separation**:

| Component | Common.Shared (Generic) | Infrastructure (Microservice-Specific) |
|-----------|------------------------|----------------------------------------|
| Redis | Generic IDistributedCacheService, RedisCacheService | Repository implementations using the cache |
| Service Bus | Generic IMessagePublisher, ServiceBusPublisher | Event publishers for domain events |
| HTTP Clients | Generic resilience policies, auth handlers | Specific external service adapters |
| Database | None | DbContext, entity configurations, repositories |
| OpenTelemetry | Generic configuration, ActivitySource helpers | Microservice-specific metrics and activities |
| Logging | Generic high-performance delegates | Microservice-specific log messages |

This separation ensures:
1. Common infrastructure concerns are reusable via NuGet
2. Microservice-specific business logic stays in Infrastructure
3. Easy to update shared components across all services
4. Clear boundaries between generic and specific code

### 7. OpenTelemetry Observability

**Purpose**: Comprehensive observability with distributed tracing, metrics, and logging.

**Components**:
- **Distributed Tracing**: Track requests across microservices
- **Metrics**: Performance counters and business metrics
- **Logging**: Structured logs correlated with traces
- **Exporters**: Azure Monitor, Jaeger, or Prometheus

**Configuration**:
```csharp
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var serviceName = configuration["ServiceName"];
        var serviceVersion = configuration["ServiceVersion"];
        
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .AddSource(serviceName)
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName, serviceVersion: serviceVersion))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = (httpContext) => 
                            !httpContext.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                    })
                    .AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
                    });
            })
            .WithMetrics(builder =>
            {
                builder
                    .AddMeter(serviceName)
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName, serviceVersion: serviceVersion))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddAzureMonitorMetricExporter(options =>
                    {
                        options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
                    });
            });
        
        return services;
    }
}
```

**Custom Metrics Example**:
```csharp
public class InspectionMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _inspectionsCreated;
    private readonly Counter<long> _inspectionsCompleted;
    private readonly Histogram<double> _inspectionDuration;
    
    public InspectionMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("InspectionService");
        
        _inspectionsCreated = _meter.CreateCounter<long>(
            "inspections.created",
            description: "Number of inspections created");
        
        _inspectionsCompleted = _meter.CreateCounter<long>(
            "inspections.completed",
            description: "Number of inspections completed");
        
        _inspectionDuration = _meter.CreateHistogram<double>(
            "inspection.duration",
            unit: "minutes",
            description: "Duration of inspections");
    }
    
    public void RecordInspectionCreated() => _inspectionsCreated.Add(1);
    
    public void RecordInspectionCompleted(TimeSpan duration)
    {
        _inspectionsCompleted.Add(1);
        _inspectionDuration.Record(duration.TotalMinutes);
    }
}
```

**Activity Source for Distributed Tracing**:
```csharp
public class InspectionActivitySource
{
    private static readonly ActivitySource ActivitySource = 
        new("InspectionService", "1.0.0");
    
    public static Activity? StartCreateInspectionActivity(Guid inspectionId)
    {
        var activity = ActivitySource.StartActivity("CreateInspection", ActivityKind.Internal);
        activity?.SetTag("inspection.id", inspectionId);
        return activity;
    }
    
    public static Activity? StartCompleteInspectionActivity(Guid inspectionId)
    {
        var activity = ActivitySource.StartActivity("CompleteInspection", ActivityKind.Internal);
        activity?.SetTag("inspection.id", inspectionId);
        return activity;
    }
}
```

### 8. Service-to-Service Authentication

**Purpose**: Secure internal communication between microservices using Azure Entra ID with least privilege access.

**Approach**: Use managed identities and OAuth 2.0 client credentials flow for service-to-service authentication.

**Configuration**:
```csharp
public static class ServiceAuthenticationExtensions
{
    public static IServiceCollection AddServiceToServiceAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // For calling other services
        services.AddHttpClient("InspectionService", client =>
        {
            client.BaseAddress = new Uri(configuration["Services:InspectionService:BaseUrl"]);
        })
        .AddHttpMessageHandler<ServiceAuthenticationHandler>();
        
        // For validating incoming service calls
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer("ServiceAuth", options =>
            {
                options.Authority = configuration["AzureAd:Authority"];
                options.Audience = configuration["AzureAd:ServiceAudience"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudiences = configuration.GetSection("AzureAd:ValidAudiences").Get<string[]>()
                };
            });
        
        return services;
    }
}

public class ServiceAuthenticationHandler : DelegatingHandler
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IConfiguration _configuration;
    
    public ServiceAuthenticationHandler(
        ITokenAcquisition tokenAcquisition,
        IConfiguration configuration)
    {
        _tokenAcquisition = tokenAcquisition;
        _configuration = configuration;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var scopes = new[] { _configuration["AzureAd:ServiceScope"] };
        var token = await _tokenAcquisition.GetAccessTokenForAppAsync(scopes);
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        return await base.SendAsync(request, cancellationToken);
    }
}
```

**Authorization Policies with Least Privilege**:
```csharp
public static class AuthorizationPolicies
{
    public static void AddInspectionPolicies(this AuthorizationOptions options)
    {
        // User policies (from API Gateway)
        options.AddPolicy("CanCreateInspection", policy =>
            policy.RequireClaim("role", "Inspector", "Admin"));
        
        options.AddPolicy("CanCompleteInspection", policy =>
            policy.RequireClaim("role", "Inspector"));
        
        options.AddPolicy("CanViewAllInspections", policy =>
            policy.RequireClaim("role", "Admin", "Supervisor"));
        
        // Service-to-service policies
        options.AddPolicy("ReportingServiceAccess", policy =>
            policy.RequireClaim("azp", "reporting-service-client-id")
                  .RequireClaim("scp", "Inspections.Read"));
        
        options.AddPolicy("MasterDataServiceAccess", policy =>
            policy.RequireClaim("azp", "masterdata-service-client-id")
                  .RequireClaim("scp", "Inspections.Write"));
    }
}
```

### 9. Central Package Management

**Purpose**: Manage NuGet package versions centrally across all projects.

**Directory.Packages.props** (at solution root):
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- ASP.NET Core -->
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
    
    <!-- MediatR -->
    <PackageVersion Include="MediatR" Version="12.2.0" />
    
    <!-- FluentValidation -->
    <PackageVersion Include="FluentValidation" Version="11.9.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
    
    <!-- Entity Framework Core -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
    
    <!-- Redis -->
    <PackageVersion Include="StackExchange.Redis" Version="2.7.10" />
    
    <!-- Polly -->
    <PackageVersion Include="Polly" Version="8.2.0" />
    <PackageVersion Include="Polly.Extensions.Http" Version="3.0.0" />
    
    <!-- Serilog -->
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="5.0.1" />
    <PackageVersion Include="Serilog.Sinks.ApplicationInsights" Version="4.0.0" />
    
    <!-- OpenTelemetry -->
    <PackageVersion Include="OpenTelemetry" Version="1.7.0" />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.7.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.7.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.7.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.0.0-beta.9" />
    <PackageVersion Include="OpenTelemetry.Exporter.AzureMonitor" Version="1.2.0" />
    
    <!-- Azure -->
    <PackageVersion Include="Azure.Identity" Version="1.10.4" />
    <PackageVersion Include="Azure.Messaging.ServiceBus" Version="7.17.0" />
    <PackageVersion Include="Microsoft.Identity.Web" Version="2.15.5" />
    
    <!-- YARP -->
    <PackageVersion Include="Yarp.ReverseProxy" Version="2.1.0" />
    
    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.6.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.5.5" />
    <PackageVersion Include="FluentAssertions" Version="6.12.0" />
    <PackageVersion Include="NSubstitute" Version="5.1.0" />
    <PackageVersion Include="Testcontainers" Version="3.6.0" />
    <PackageVersion Include="Testcontainers.PostgreSql" Version="3.6.0" />
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
    <PackageVersion Include="FsCheck" Version="2.16.6" />
    <PackageVersion Include="FsCheck.Xunit" Version="2.16.6" />
  </ItemGroup>
</Project>
```

**Project .csproj files** (no version specified):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="MediatR" />
    <PackageReference Include="FluentValidation" />
  </ItemGroup>
</Project>
```

### 10. Sample CRUD Operations

**Complete CRUD Example for Inspection Entity**

#### Create Operation

**Command**:
```csharp
// Application/Inspections/Commands/CreateInspection/CreateInspectionCommand.cs
public record CreateInspectionCommand : IRequest<Result<Guid>>
{
    public string Title { get; init; }
    public string Description { get; init; }
    public Guid InspectorId { get; init; }
    public DateTime ScheduledDate { get; init; }
    public List<CreateInspectionItemDto> Items { get; init; }
}

public record CreateInspectionItemDto
{
    public string ItemName { get; init; }
    public string ChecklistCriteria { get; init; }
}
```

**Validator**:
```csharp
// Application/Inspections/Commands/CreateInspection/CreateInspectionCommandValidator.cs
public class CreateInspectionCommandValidator : AbstractValidator<CreateInspectionCommand>
{
    public CreateInspectionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");
        
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");
        
        RuleFor(x => x.InspectorId)
            .NotEmpty().WithMessage("Inspector ID is required");
        
        RuleFor(x => x.ScheduledDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Scheduled date must be in the future");
        
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one inspection item is required");
        
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ItemName)
                .NotEmpty().WithMessage("Item name is required");
            item.RuleFor(x => x.ChecklistCriteria)
                .NotEmpty().WithMessage("Checklist criteria is required");
        });
    }
}
```

**Handler**:
```csharp
// Application/Inspections/Commands/CreateInspection/CreateInspectionCommandHandler.cs
public class CreateInspectionCommandHandler : IRequestHandler<CreateInspectionCommand, Result<Guid>>
{
    private readonly IInspectionRepository _repository;
    private readonly ILogger<CreateInspectionCommandHandler> _logger;
    private readonly InspectionMetrics _metrics;
    
    public CreateInspectionCommandHandler(
        IInspectionRepository repository,
        ILogger<CreateInspectionCommandHandler> logger,
        InspectionMetrics metrics)
    {
        _repository = repository;
        _logger = logger;
        _metrics = metrics;
    }
    
    public async Task<Result<Guid>> Handle(
        CreateInspectionCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = InspectionActivitySource.StartCreateInspectionActivity(Guid.NewGuid());
        
        try
        {
            var inspection = Inspection.Create(
                request.Title,
                request.Description,
                request.InspectorId,
                request.ScheduledDate);
            
            foreach (var itemDto in request.Items)
            {
                var item = InspectionItem.Create(
                    inspection.Id,
                    itemDto.ItemName,
                    itemDto.ChecklistCriteria);
                inspection.AddItem(item);
            }
            
            await _repository.AddAsync(inspection, cancellationToken);
            
            _logger.LogInspectionCreation(inspection.Id, request.InspectorId);
            _metrics.RecordInspectionCreated();
            
            activity?.SetTag("inspection.items.count", request.Items.Count);
            
            return Result<Guid>.Success(inspection.Id);
        }
        catch (Exception ex)
        {
            _logger.LogInspectionRetrievalFailed(ex, Guid.Empty);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result<Guid>.Failure($"Failed to create inspection: {ex.Message}");
        }
    }
}
```

**Controller**:
```csharp
// Api/Controllers/Inspections/InspectionsController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InspectionsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public InspectionsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    [Authorize(Policy = "CanCreateInspection")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInspectionCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }
}
```

#### Read Operations

**Get By ID Query**:
```csharp
// Application/Inspections/Queries/GetInspectionById/GetInspectionByIdQuery.cs
public record GetInspectionByIdQuery(Guid Id) : IRequest<Result<InspectionDto>>;

// Handler
public class GetInspectionByIdQueryHandler : IRequestHandler<GetInspectionByIdQuery, Result<InspectionDto>>
{
    private readonly IInspectionRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<GetInspectionByIdQueryHandler> _logger;
    
    public async Task<Result<InspectionDto>> Handle(
        GetInspectionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"inspection:{request.Id}";
        
        // Try cache first
        var cached = await _cache.GetAsync<InspectionDto>(cacheKey, cancellationToken);
        if (cached != null)
            return Result<InspectionDto>.Success(cached);
        
        // Fetch from repository
        var result = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (!result.IsSuccess)
            return Result<InspectionDto>.Failure(result.Error);
        
        var dto = MapToDto(result.Value);
        
        // Cache for 15 minutes
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(15), cancellationToken);
        
        return Result<InspectionDto>.Success(dto);
    }
}
```

**List Query**:
```csharp
// Application/Inspections/Queries/ListInspections/ListInspectionsQuery.cs
public record ListInspectionsQuery : IRequest<Result<PagedResult<InspectionSummaryDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Status { get; init; }
    public Guid? InspectorId { get; init; }
}

// Handler
public class ListInspectionsQueryHandler : IRequestHandler<ListInspectionsQuery, Result<PagedResult<InspectionSummaryDto>>>
{
    private readonly IInspectionRepository _repository;
    
    public async Task<Result<PagedResult<InspectionSummaryDto>>> Handle(
        ListInspectionsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Status,
            request.InspectorId,
            cancellationToken);
        
        if (!result.IsSuccess)
            return Result<PagedResult<InspectionSummaryDto>>.Failure(result.Error);
        
        var dtos = result.Value.Items.Select(MapToSummaryDto).ToList();
        
        var pagedResult = new PagedResult<InspectionSummaryDto>
        {
            Items = dtos,
            TotalCount = result.Value.TotalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        
        return Result<PagedResult<InspectionSummaryDto>>.Success(pagedResult);
    }
}
```

**Controller**:
```csharp
[HttpGet("{id:guid}")]
[ProducesResponseType(typeof(InspectionDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetById(Guid id)
{
    var result = await _mediator.Send(new GetInspectionByIdQuery(id));
    
    if (!result.IsSuccess)
        return NotFound(new { error = result.Error });
    
    return Ok(result.Value);
}

[HttpGet]
[ProducesResponseType(typeof(PagedResult<InspectionSummaryDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> List([FromQuery] ListInspectionsQuery query)
{
    var result = await _mediator.Send(query);
    
    if (!result.IsSuccess)
        return BadRequest(new { error = result.Error });
    
    return Ok(result.Value);
}
```

#### Update Operation

**Command**:
```csharp
// Application/Inspections/Commands/UpdateInspection/UpdateInspectionCommand.cs
public record UpdateInspectionCommand : IRequest<Result<Unit>>
{
    public Guid Id { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public DateTime ScheduledDate { get; init; }
}

// Handler
public class UpdateInspectionCommandHandler : IRequestHandler<UpdateInspectionCommand, Result<Unit>>
{
    private readonly IInspectionRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<UpdateInspectionCommandHandler> _logger;
    
    public async Task<Result<Unit>> Handle(
        UpdateInspectionCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (!result.IsSuccess)
            return Result<Unit>.Failure(result.Error);
        
        var inspection = result.Value;
        
        inspection.Update(request.Title, request.Description, request.ScheduledDate);
        
        await _repository.UpdateAsync(inspection, cancellationToken);
        
        // Invalidate cache
        await _cache.RemoveAsync($"inspection:{request.Id}", cancellationToken);
        
        _logger.LogInformation("Updated inspection {InspectionId}", request.Id);
        
        return Result<Unit>.Success(Unit.Value);
    }
}
```

**Controller**:
```csharp
[HttpPut("{id:guid}")]
[Authorize(Policy = "CanCreateInspection")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInspectionCommand command)
{
    if (id != command.Id)
        return BadRequest(new { error = "ID mismatch" });
    
    var result = await _mediator.Send(command);
    
    if (!result.IsSuccess)
        return NotFound(new { error = result.Error });
    
    return NoContent();
}
```

#### Delete Operation

**Command**:
```csharp
// Application/Inspections/Commands/DeleteInspection/DeleteInspectionCommand.cs
public record DeleteInspectionCommand(Guid Id) : IRequest<Result<Unit>>;

// Handler
public class DeleteInspectionCommandHandler : IRequestHandler<DeleteInspectionCommand, Result<Unit>>
{
    private readonly IInspectionRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<DeleteInspectionCommandHandler> _logger;
    
    public async Task<Result<Unit>> Handle(
        DeleteInspectionCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (!result.IsSuccess)
            return Result<Unit>.Failure(result.Error);
        
        await _repository.DeleteAsync(request.Id, cancellationToken);
        
        // Invalidate cache
        await _cache.RemoveAsync($"inspection:{request.Id}", cancellationToken);
        
        _logger.LogInformation("Deleted inspection {InspectionId}", request.Id);
        
        return Result<Unit>.Success(Unit.Value);
    }
}
```

**Controller**:
```csharp
[HttpDelete("{id:guid}")]
[Authorize(Policy = "CanCreateInspection")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Delete(Guid id)
{
    var result = await _mediator.Send(new DeleteInspectionCommand(id));
    
    if (!result.IsSuccess)
        return NotFound(new { error = result.Error });
    
    return NoContent();
}
```

### 11. API Gateway (YARP)

**Purpose**: Single entry point for all external requests with authentication and routing.

**Components**:
- **Authentication Middleware**: Azure Entra External JWT validation
- **Authorization Policies**: Role and claim-based authorization
- **Routing Configuration**: Route definitions to microservices
- **Rate Limiting**: Request throttling per client
- **Logging**: Request/response logging

**Configuration Example**:
```json
{
  "ReverseProxy": {
    "Routes": {
      "inspection-route": {
        "ClusterId": "inspection-cluster",
        "Match": {
          "Path": "/api/inspections/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/inspections/{**catch-all}" }
        ]
      }
    },
    "Clusters": {
      "inspection-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://inspection-service:80"
          }
        }
      }
    }
  }
}
```

## Data Models

### Domain Entities

**Inspection Entity**:
```csharp
public class Inspection : Entity
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public InspectionStatus Status { get; private set; }
    public Guid InspectorId { get; private set; }
    public DateTime ScheduledDate { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public List<InspectionItem> Items { get; private set; }
    
    // Factory method
    public static Inspection Create(string title, string description, 
        Guid inspectorId, DateTime scheduledDate)
    {
        // Business rules validation
        // Return new instance
    }
    
    // Domain methods
    public void Complete()
    {
        // Business logic for completing inspection
        Status = InspectionStatus.Completed;
        CompletedDate = DateTime.UtcNow;
        AddDomainEvent(new InspectionCompletedEvent(Id));
    }
}
```

**InspectionItem Entity**:
```csharp
public class InspectionItem : Entity
{
    public Guid Id { get; private set; }
    public Guid InspectionId { get; private set; }
    public string ItemName { get; private set; }
    public string ChecklistCriteria { get; private set; }
    public bool IsPassed { get; private set; }
    public string Notes { get; private set; }
    
    public void MarkAsPassed(string notes)
    {
        IsPassed = true;
        Notes = notes;
    }
    
    public void MarkAsFailed(string notes)
    {
        IsPassed = false;
        Notes = notes;
    }
}
```

**InspectionStatus Value Object**:
```csharp
public class InspectionStatus : ValueObject
{
    public string Value { get; private set; }
    
    public static InspectionStatus Draft = new("Draft");
    public static InspectionStatus Scheduled = new("Scheduled");
    public static InspectionStatus InProgress = new("InProgress");
    public static InspectionStatus Completed = new("Completed");
    public static InspectionStatus Cancelled = new("Cancelled");
    
    private InspectionStatus(string value)
    {
        Value = value;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

### Application DTOs

**InspectionDto**:
```csharp
public record InspectionDto
{
    public Guid Id { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public string Status { get; init; }
    public Guid InspectorId { get; init; }
    public DateTime ScheduledDate { get; init; }
    public DateTime? CompletedDate { get; init; }
    public List<InspectionItemDto> Items { get; init; }
}
```

### Infrastructure Persistence Models

**EF Core Configuration**:
```csharp
public class InspectionConfiguration : IEntityTypeConfiguration<Inspection>
{
    public void Configure(EntityTypeBuilder<Inspection> builder)
    {
        builder.ToTable("Inspections");
        
        builder.HasKey(i => i.Id);
        
        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(i => i.Description)
            .HasMaxLength(2000);
        
        builder.OwnsOne(i => i.Status, status =>
        {
            status.Property(s => s.Value)
                .HasColumnName("Status")
                .IsRequired()
                .HasMaxLength(50);
        });
        
        builder.HasMany(i => i.Items)
            .WithOne()
            .HasForeignKey(item => item.InspectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Cache Models

**Cached Inspection Summary**:
```csharp
public record CachedInspectionSummary
{
    public Guid Id { get; init; }
    public string Title { get; init; }
    public string Status { get; init; }
    public DateTime ScheduledDate { get; init; }
    public string CacheKey => $"inspection:summary:{Id}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*


### Property 1: Domain Layer Has Zero External Dependencies

*For any* microservice Domain project, the .csproj file should contain zero PackageReference elements except optionally Shared.Kernel.

**Validates: Requirements 1.2**

### Property 2: Dependency Flow Is Unidirectional Inward

*For any* microservice, the project references should follow this pattern: Api references Infrastructure, Application, and Domain; Infrastructure references Application; Application references Domain; Domain references nothing (except optional Shared.Kernel).

**Validates: Requirements 1.3, 1.4, 1.5, 2.4**

### Property 3: MediatR Processes All Commands and Queries

*For any* command or query type, when sent via MediatR, it should be processed by a handler that implements IRequestHandler<TRequest, TResponse>.

**Validates: Requirements 3.2, 3.3**

### Property 4: All Application Interfaces Have Infrastructure Implementations

*For any* interface defined in the Application layer (repositories, services, adapters), there should be a corresponding implementation class in the Infrastructure layer.

**Validates: Requirements 4.2, 4.4**

### Property 5: All Services Are Registered in DI Container

*For any* interface defined in the Application layer, it should be registered in the dependency injection container in Program.cs with its corresponding Infrastructure implementation.

**Validates: Requirements 4.5, 13.1**

### Property 6: External API Access Only in Infrastructure

*For any* HTTP client usage or external API call, it should only occur in classes within the Infrastructure layer, never in Domain or Application layers.

**Validates: Requirements 5.1**

### Property 7: Adapters Have Resilience Policies

*For any* adapter class in Infrastructure that makes external HTTP calls, it should have Polly resilience policies configured (retry, circuit breaker, or timeout).

**Validates: Requirements 5.3**

### Property 8: Master Data Changes Publish Events

*For any* master data update operation, an event should be published to notify dependent services of the change.

**Validates: Requirements 7.2**

### Property 9: Cache-Aside Pattern for Frequently Accessed Data

*For any* query marked as cacheable, the system should first check Redis cache, and on cache miss, retrieve from the source and populate the cache with appropriate TTL.

**Validates: Requirements 8.2, 8.3**

### Property 10: Unauthenticated Requests Are Rejected

*For any* external request to the API Gateway without a valid JWT token from Azure Entra External, the gateway should reject the request with 401 Unauthorized status.

**Validates: Requirements 9.2, 16.2**

### Property 11: Authenticated Requests Are Authorized

*For any* authenticated request to a protected endpoint, the API Gateway should evaluate authorization policies based on user claims before routing to the microservice.

**Validates: Requirements 9.3, 16.5**

### Property 12: Valid Requests Are Routed Correctly

*For any* authenticated and authorized request, the API Gateway should route it to the appropriate microservice based on the path configuration in appsettings.json.

**Validates: Requirements 9.4**

### Property 13: Internal Service Calls Require No Authentication

*For any* internal service-to-service HTTP call within the AKS cluster, no authentication headers should be required or validated.

**Validates: Requirements 9.7**

### Property 14: Structured Logging Uses Structured Properties

*For any* log statement in the codebase, it should use Serilog's structured logging syntax with named properties rather than string interpolation.

**Validates: Requirements 10.2**

### Property 15: Domain Tests Have No Infrastructure Dependencies

*For any* test in the Domain test project, it should have no dependencies on Infrastructure packages (no EF Core, no HTTP clients, no external services).

**Validates: Requirements 12.1**

### Property 16: User Claims Are Extracted from Valid Tokens

*For any* valid JWT token from Azure Entra External, the system should extract user claims and make them available in the HttpContext.User.Claims collection.

**Validates: Requirements 16.4**

### Property 17: Each Microservice Has Kubernetes Manifests

*For any* microservice in the system, there should be corresponding Kubernetes manifest files (Deployment, Service, and optionally HPA).

**Validates: Requirements 17.2**

### Property 18: Internal Microservices Use ClusterIP Service Type

*For any* microservice except the API Gateway, the Kubernetes Service manifest should specify type: ClusterIP to keep it internal to the cluster.

**Validates: Requirements 17.4**

### Property 19: All Microservices Have Health Checks Configured

*For any* microservice, the Kubernetes Deployment manifest should include both livenessProbe and readinessProbe configurations.

**Validates: Requirements 17.5**

### Property 20: All Microservices Have Resource Limits

*For any* microservice, the Kubernetes Deployment manifest should specify both resource requests and limits for CPU and memory.

**Validates: Requirements 17.6**

## Error Handling

### Domain Layer Error Handling

**Domain Exceptions**: Create custom exception types for domain rule violations.

```csharp
public class InspectionDomainException : Exception
{
    public InspectionDomainException(string message) : base(message) { }
}

public class InvalidInspectionStateException : InspectionDomainException
{
    public InvalidInspectionStateException(string message) : base(message) { }
}
```

**Guard Clauses**: Use guard clauses in domain methods to validate business rules.

```csharp
public void Complete()
{
    if (Status == InspectionStatus.Completed)
        throw new InvalidInspectionStateException("Inspection is already completed");
    
    if (Status == InspectionStatus.Cancelled)
        throw new InvalidInspectionStateException("Cannot complete a cancelled inspection");
    
    if (!Items.All(i => i.IsPassed))
        throw new InvalidInspectionStateException("Cannot complete inspection with failed items");
    
    Status = InspectionStatus.Completed;
    CompletedDate = DateTime.UtcNow;
}
```

### Application Layer Error Handling

**Result Pattern**: Use Result<T> for operation outcomes instead of throwing exceptions.

```csharp
public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T Value { get; init; }
    public string Error { get; init; }
    
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}
```

**Validation**: Use FluentValidation for command and query validation.

```csharp
public class CreateInspectionCommandValidator : AbstractValidator<CreateInspectionCommand>
{
    public CreateInspectionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");
        
        RuleFor(x => x.ScheduledDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Scheduled date must be in the future");
    }
}
```

**MediatR Pipeline Behavior**: Add validation behavior to automatically validate commands.

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();
        
        if (failures.Any())
            throw new ValidationException(failures);
        
        return await next();
    }
}
```

### Infrastructure Layer Error Handling

**Resilience Policies**: Use Polly for retry, circuit breaker, and timeout policies.

```csharp
public class ExternalServiceAdapter : IExternalService
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;
    
    public ExternalServiceAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _resiliencePolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            .WrapAsync(Policy
                .Handle<HttpRequestException>()
                .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1)));
    }
    
    public async Task<Result<ExternalData>> GetDataAsync(string id)
    {
        try
        {
            var response = await _resiliencePolicy.ExecuteAsync(() => 
                _httpClient.GetAsync($"/api/data/{id}"));
            
            if (!response.IsSuccessStatusCode)
                return Result<ExternalData>.Failure($"External service returned {response.StatusCode}");
            
            var data = await response.Content.ReadFromJsonAsync<ExternalData>();
            return Result<ExternalData>.Success(data);
        }
        catch (Exception ex)
        {
            return Result<ExternalData>.Failure($"Failed to retrieve data: {ex.Message}");
        }
    }
}
```

**Database Error Handling**: Handle EF Core exceptions and translate to domain errors.

```csharp
public class InspectionRepository : IInspectionRepository
{
    private readonly ApplicationDbContext _context;
    
    public async Task<Result<Inspection>> GetByIdAsync(Guid id)
    {
        try
        {
            var inspection = await _context.Inspections
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);
            
            if (inspection == null)
                return Result<Inspection>.Failure($"Inspection with ID {id} not found");
            
            return Result<Inspection>.Success(inspection);
        }
        catch (DbUpdateException ex)
        {
            return Result<Inspection>.Failure($"Database error: {ex.Message}");
        }
    }
}
```

### API Layer Error Handling

**Global Exception Middleware**: Catch all unhandled exceptions and return consistent error responses.

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred");
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (InspectionDomainException ex)
        {
            _logger.LogWarning(ex, "Domain error occurred");
            await HandleDomainExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleUnhandledExceptionAsync(context, ex);
        }
    }
    
    private static Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsJsonAsync(new
        {
            type = "ValidationError",
            errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
        });
    }
    
    private static Task HandleDomainExceptionAsync(HttpContext context, InspectionDomainException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsJsonAsync(new
        {
            type = "DomainError",
            message = ex.Message
        });
    }
    
    private static Task HandleUnhandledExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return context.Response.WriteAsJsonAsync(new
        {
            type = "InternalError",
            message = "An unexpected error occurred"
        });
    }
}
```

## Testing Strategy

### Overview

The testing strategy follows a layered approach that matches the Clean Architecture structure. Each layer has specific testing requirements and techniques that ensure comprehensive coverage while maintaining fast, reliable tests.

### Domain Layer Testing

**Approach**: Pure unit tests with zero external dependencies.

**Focus**:
- Entity behavior and business rules
- Value object equality and immutability
- Domain service logic
- Domain event generation

**Tools**:
- xUnit for test framework
- FluentAssertions for readable assertions
- No mocking required (pure domain logic)

**Example Test**:
```csharp
public class InspectionTests
{
    [Fact]
    public void Complete_WithAllItemsPassed_ShouldSetStatusToCompleted()
    {
        // Arrange
        var inspection = Inspection.Create("Test", "Description", Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        var item = InspectionItem.Create(inspection.Id, "Item 1", "Criteria");
        item.MarkAsPassed("Looks good");
        inspection.AddItem(item);
        
        // Act
        inspection.Complete();
        
        // Assert
        inspection.Status.Should().Be(InspectionStatus.Completed);
        inspection.CompletedDate.Should().NotBeNull();
    }
    
    [Fact]
    public void Complete_WithFailedItems_ShouldThrowInvalidInspectionStateException()
    {
        // Arrange
        var inspection = Inspection.Create("Test", "Description", Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        var item = InspectionItem.Create(inspection.Id, "Item 1", "Criteria");
        item.MarkAsFailed("Does not meet criteria");
        inspection.AddItem(item);
        
        // Act & Assert
        inspection.Invoking(i => i.Complete())
            .Should().Throw<InvalidInspectionStateException>()
            .WithMessage("Cannot complete inspection with failed items");
    }
}
```

### Application Layer Testing

**Approach**: Unit tests with mocked dependencies.

**Focus**:
- Command and query handler logic
- MediatR pipeline behaviors
- Validation rules
- Interface contracts

**Tools**:
- xUnit for test framework
- FluentAssertions for assertions
- NSubstitute or Moq for mocking
- Minimum 100 iterations for property-based tests

**Example Test**:
```csharp
public class CreateInspectionCommandHandlerTests
{
    private readonly IInspectionRepository _repository;
    private readonly CreateInspectionCommandHandler _handler;
    
    public CreateInspectionCommandHandlerTests()
    {
        _repository = Substitute.For<IInspectionRepository>();
        _handler = new CreateInspectionCommandHandler(_repository);
    }
    
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateInspection()
    {
        // Arrange
        var command = new CreateInspectionCommand
        {
            Title = "Test Inspection",
            Description = "Test Description",
            InspectorId = Guid.NewGuid(),
            ScheduledDate = DateTime.UtcNow.AddDays(1)
        };
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Inspection>());
    }
}
```

### Infrastructure Layer Testing

**Approach**: Integration tests with real dependencies.

**Focus**:
- EF Core repository implementations
- Database queries and migrations
- Redis cache operations
- External service adapters
- Message bus publishers and consumers

**Tools**:
- xUnit for test framework
- FluentAssertions for assertions
- Testcontainers for Docker-based dependencies
- WebApplicationFactory for API testing

**Example Test**:
```csharp
public class InspectionRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private ApplicationDbContext _context;
    private InspectionRepository _repository;
    
    public InspectionRepositoryTests()
    {
        _dbContainer = new PostgreSqlBuilder().Build();
    }
    
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        await _context.Database.MigrateAsync();
        
        _repository = new InspectionRepository(_context);
    }
    
    [Fact]
    public async Task GetByIdAsync_ExistingInspection_ShouldReturnInspection()
    {
        // Arrange
        var inspection = Inspection.Create("Test", "Description", Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        await _repository.AddAsync(inspection);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetByIdAsync(inspection.Id);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(inspection.Id);
    }
    
    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}
```

### API Layer Testing

**Approach**: Integration tests using WebApplicationFactory.

**Focus**:
- Controller endpoints
- Middleware pipeline
- Authentication and authorization
- Request/response serialization

**Example Test**:
```csharp
public class InspectionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public InspectionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task CreateInspection_ValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var request = new CreateInspectionRequest
        {
            Title = "Test Inspection",
            Description = "Test Description",
            InspectorId = Guid.NewGuid(),
            ScheduledDate = DateTime.UtcNow.AddDays(1)
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/inspections", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var inspection = await response.Content.ReadFromJsonAsync<InspectionDto>();
        inspection.Title.Should().Be(request.Title);
    }
}
```

### Architecture Tests

**Approach**: Unit tests using NetArchTest to enforce dependency rules.

**Focus**:
- Dependency direction enforcement
- Layer isolation verification
- Naming convention compliance
- Package reference restrictions

**Example Test**:
```csharp
public class ArchitectureTests
{
    private const string DomainNamespace = "InspectionService.Domain";
    private const string ApplicationNamespace = "InspectionService.Application";
    private const string InfrastructureNamespace = "InspectionService.Infrastructure";
    private const string ApiNamespace = "InspectionService.Api";
    
    [Fact]
    public void Domain_Should_Not_HaveDependencyOnOtherLayers()
    {
        // Arrange
        var assembly = typeof(Inspection).Assembly;
        
        // Act
        var result = Types.InAssembly(assembly)
            .That().ResideInNamespace(DomainNamespace)
            .ShouldNot().HaveDependencyOn(ApplicationNamespace)
            .And().ShouldNot().HaveDependencyOn(InfrastructureNamespace)
            .And().ShouldNot().HaveDependencyOn(ApiNamespace)
            .GetResult();
        
        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Application_Should_Not_HaveDependencyOnInfrastructureOrApi()
    {
        // Arrange
        var assembly = typeof(CreateInspectionCommand).Assembly;
        
        // Act
        var result = Types.InAssembly(assembly)
            .That().ResideInNamespace(ApplicationNamespace)
            .ShouldNot().HaveDependencyOn(InfrastructureNamespace)
            .And().ShouldNot().HaveDependencyOn(ApiNamespace)
            .GetResult();
        
        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
    
    [Fact]
    public void Controllers_Should_HaveSuffix()
    {
        // Arrange
        var assembly = typeof(Program).Assembly;
        
        // Act
        var result = Types.InAssembly(assembly)
            .That().Inherit(typeof(ControllerBase))
            .Should().HaveNameEndingWith("Controller")
            .GetResult();
        
        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
}
```

### Property-Based Testing

**Approach**: Use FsCheck or similar library for property-based tests.

**Configuration**:
- Minimum 100 iterations per property test
- Each test tagged with: **Feature: digital-inspection-system, Property {number}: {property_text}**

**Example Test**:
```csharp
// Feature: digital-inspection-system, Property 2: Dependency Flow Is Unidirectional Inward
[Property(MaxTest = 100)]
public Property AllMicroservices_ShouldFollowDependencyRules()
{
    return Prop.ForAll(
        Arb.From<string>().Generator.Select(name => $"{name}Service"),
        serviceName =>
        {
            var domainProject = LoadProject($"{serviceName}.Domain");
            var applicationProject = LoadProject($"{serviceName}.Application");
            var infrastructureProject = LoadProject($"{serviceName}.Infrastructure");
            var apiProject = LoadProject($"{serviceName}.Api");
            
            var domainReferences = GetProjectReferences(domainProject);
            var applicationReferences = GetProjectReferences(applicationProject);
            var infrastructureReferences = GetProjectReferences(infrastructureProject);
            var apiReferences = GetProjectReferences(apiProject);
            
            return domainReferences.Count == 0 &&
                   applicationReferences.Single().Contains("Domain") &&
                   infrastructureReferences.Single().Contains("Application") &&
                   apiReferences.Count == 3;
        });
}
```

### Test Coverage Goals

- **Domain Layer**: 90%+ code coverage (pure logic, easy to test)
- **Application Layer**: 80%+ code coverage (handlers and validation)
- **Infrastructure Layer**: 70%+ code coverage (integration tests are slower)
- **API Layer**: 60%+ code coverage (thin layer, mostly integration tests)

### Continuous Integration

All tests should run in CI pipeline:
1. Architecture tests (fast, fail fast)
2. Domain unit tests (fast)
3. Application unit tests (fast)
4. Infrastructure integration tests (slower, use Testcontainers)
5. API integration tests (slower)
6. Property-based tests (100+ iterations, can be slow)

## Implementation Notes

### Project Creation Script

Create a PowerShell script to scaffold new microservices:

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$ServiceName
)

$basePath = "src/$ServiceName"

# Create projects
dotnet new classlib -n "$ServiceName.Domain" -o "$basePath/$ServiceName.Domain"
dotnet new classlib -n "$ServiceName.Application" -o "$basePath/$ServiceName.Application"
dotnet new classlib -n "$ServiceName.Infrastructure" -o "$basePath/$ServiceName.Infrastructure"
dotnet new webapi -n "$ServiceName.Api" -o "$basePath/$ServiceName.Api"
dotnet new classlib -n "$ServiceName.Shared.Kernel" -o "$basePath/$ServiceName.Shared.Kernel"

# Add project references
dotnet add "$basePath/$ServiceName.Application" reference "$basePath/$ServiceName.Domain"
dotnet add "$basePath/$ServiceName.Infrastructure" reference "$basePath/$ServiceName.Application"
dotnet add "$basePath/$ServiceName.Api" reference "$basePath/$ServiceName.Domain"
dotnet add "$basePath/$ServiceName.Api" reference "$basePath/$ServiceName.Application"
dotnet add "$basePath/$ServiceName.Api" reference "$basePath/$ServiceName.Infrastructure"

# Add NuGet packages
dotnet add "$basePath/$ServiceName.Application" package MediatR
dotnet add "$basePath/$ServiceName.Application" package FluentValidation
dotnet add "$basePath/$ServiceName.Infrastructure" package Microsoft.EntityFrameworkCore
dotnet add "$basePath/$ServiceName.Infrastructure" package StackExchange.Redis
dotnet add "$basePath/$ServiceName.Infrastructure" package Polly
dotnet add "$basePath/$ServiceName.Api" package Serilog.AspNetCore

Write-Host "Microservice $ServiceName created successfully!"
```

### Kubernetes Deployment Template

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: inspection-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: inspection-service
  template:
    metadata:
      labels:
        app: inspection-service
    spec:
      containers:
      - name: inspection-service
        image: myregistry.azurecr.io/inspection-service:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-secrets
              key: connection-string
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: inspection-service
spec:
  type: ClusterIP
  selector:
    app: inspection-service
  ports:
  - port: 80
    targetPort: 80
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: inspection-service-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: inspection-service
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### API Gateway Configuration

```csharp
// Program.cs for API Gateway
var builder = WebApplication.CreateBuilder(args);

// Add Azure Entra External authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["AzureAd:Authority"];
        options.Audience = builder.Configuration["AzureAd:ClientId"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InspectorOnly", policy =>
        policy.RequireClaim("role", "Inspector"));
});

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapReverseProxy();

app.Run();
```

This design provides a comprehensive foundation for building the digital inspection system with Clean Architecture, ensuring maintainability, testability, and scalability across all microservices.
