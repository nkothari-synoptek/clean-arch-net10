# ADR 0003: Common.Shared as Private NuGet Package

## Status

Accepted

## Context

As we build multiple microservices, we've identified several infrastructure components that are identical across services:

1. **Redis Caching**: All services need distributed caching with the same patterns
2. **Azure Service Bus**: All services publish and consume messages using the same abstractions
3. **HTTP Resilience**: All services make HTTP calls with retry, circuit breaker, and timeout policies
4. **Logging**: All services need high-performance structured logging
5. **OpenTelemetry**: All services need distributed tracing and metrics
6. **Service-to-Service Auth**: All services need to authenticate with each other using Azure Entra ID

Without a shared library, we face:
- Code duplication across microservices
- Inconsistent implementations of the same patterns
- Difficulty maintaining and updating shared functionality
- Risk of divergence as services evolve independently

We need a way to share these components while maintaining:
- Microservice independence
- Version control
- Easy updates
- Clear boundaries between shared and service-specific code

## Decision

We will create a `Common.Shared` library distributed as a private NuGet package containing reusable infrastructure components.

### What Goes in Common.Shared

**Generic, reusable infrastructure abstractions and implementations**:

1. **Caching** (`Common.Shared/Caching/`)
   - `IDistributedCacheService` interface
   - `RedisCacheService` generic implementation
   - `CacheExtensions` for DI registration

2. **Service Bus** (`Common.Shared/ServiceBus/`)
   - `IMessagePublisher` and `IMessageConsumer` interfaces
   - `ServiceBusPublisher` and `ServiceBusConsumer` generic implementations
   - `ServiceBusExtensions` for DI registration

3. **Resilience** (`Common.Shared/Resilience/`)
   - `HttpResiliencePolicies` with Polly (retry, circuit breaker, timeout)
   - `ResilienceExtensions` for IHttpClientBuilder

4. **Logging** (`Common.Shared/Logging/`)
   - `LogMessages` class with LoggerMessage.Define delegates
   - `LoggingExtensions` for configuration

5. **Observability** (`Common.Shared/Observability/`)
   - `OpenTelemetryExtensions` for tracing and metrics
   - `ActivitySourceProvider` for distributed tracing
   - `MetricsProvider` for custom metrics

6. **Authentication** (`Common.Shared/Authentication/`)
   - `ServiceAuthenticationHandler` for OAuth 2.0 client credentials
   - `ServiceAuthenticationExtensions` for DI registration

### What Stays in Microservice Infrastructure

**Microservice-specific implementations**:

1. **Repositories**: Entity-specific repository implementations
2. **DbContext**: Microservice-specific database context and configurations
3. **External Service Adapters**: Adapters for external APIs specific to the microservice
4. **Message Handlers**: Handlers for business events specific to the microservice
5. **Caching Strategies**: Microservice-specific cache key patterns and expiration policies

### Distribution

- Package hosted in Azure Artifacts (private NuGet feed)
- Semantic versioning (e.g., 1.0.0, 1.1.0, 2.0.0)
- Microservices reference specific versions
- Updates require explicit version bumps in microservices

## Consequences

### Positive

1. **Code Reuse**: Write once, use everywhere
   - No duplication of caching logic
   - No duplication of Service Bus abstractions
   - No duplication of resilience policies

2. **Consistency**: All services use the same patterns
   - Consistent error handling
   - Consistent logging
   - Consistent observability

3. **Maintainability**: Update in one place
   - Bug fixes propagate to all services
   - Performance improvements benefit all services
   - Security updates are centralized

4. **Testability**: Shared components are well-tested
   - Unit tests in Common.Shared project
   - Integration tests verify behavior
   - All microservices benefit from tested code

5. **Onboarding**: New developers learn patterns once
   - Clear examples in Common.Shared
   - Consistent usage across services
   - Less cognitive load

6. **Versioning**: Explicit version control
   - Services can upgrade at their own pace
   - Breaking changes are clearly communicated
   - Rollback is possible

### Negative

1. **Coupling**: Services depend on Common.Shared
   - Mitigation: Keep Common.Shared focused on infrastructure only
   - Mitigation: No business logic in Common.Shared
   - Mitigation: Use semantic versioning for breaking changes

2. **Versioning Complexity**: Managing versions across services
   - Mitigation: Use Central Package Management (Directory.Packages.props)
   - Mitigation: Document version compatibility
   - Mitigation: Automated dependency updates (Dependabot)

3. **Breaking Changes**: Updates may require changes in all services
   - Mitigation: Follow semantic versioning strictly
   - Mitigation: Deprecate before removing
   - Mitigation: Provide migration guides

4. **Build Dependency**: Services need access to private NuGet feed
   - Mitigation: Configure Azure Artifacts in CI/CD
   - Mitigation: Use authenticated NuGet feeds
   - Mitigation: Cache packages in build agents

5. **Testing Overhead**: Need to test Common.Shared thoroughly
   - Mitigation: Comprehensive unit and integration tests
   - Mitigation: High code coverage requirements
   - Mitigation: Property-based tests for critical components

## Alternatives Considered

### 1. Copy-Paste Code

**Approach**: Copy infrastructure code into each microservice

**Rejected because**:
- Code duplication leads to inconsistency
- Bug fixes need to be applied to every service
- Difficult to maintain as services grow
- No single source of truth

### 2. Shared Git Submodule

**Approach**: Use Git submodules to share code

**Rejected because**:
- Submodules are difficult to manage
- No versioning (always uses latest)
- Difficult to test changes before merging
- Poor developer experience

### 3. Monorepo with Shared Projects

**Approach**: All services in one repository with shared projects

**Rejected because**:
- Violates microservice independence
- Difficult to version services independently
- Large repository becomes unwieldy
- CI/CD complexity increases

### 4. Separate NuGet Package per Component

**Approach**: Multiple packages (Common.Caching, Common.ServiceBus, etc.)

**Rejected because**:
- Too many packages to manage
- Versioning becomes complex
- Dependency graph becomes complicated
- Overhead of maintaining multiple packages

## Implementation Notes

### Package Structure

```
Common.Shared/
├── ServiceBus/
│   ├── IMessagePublisher.cs
│   ├── IMessageConsumer.cs
│   ├── ServiceBusPublisher.cs
│   ├── ServiceBusConsumer.cs
│   └── ServiceBusExtensions.cs
├── Caching/
│   ├── IDistributedCacheService.cs
│   ├── RedisCacheService.cs
│   └── CacheExtensions.cs
├── Logging/
│   ├── LogMessages.cs
│   └── LoggingExtensions.cs
├── Observability/
│   ├── OpenTelemetryExtensions.cs
│   ├── ActivitySourceProvider.cs
│   └── MetricsProvider.cs
├── Authentication/
│   ├── ServiceAuthenticationHandler.cs
│   └── ServiceAuthenticationExtensions.cs
├── Resilience/
│   ├── HttpResiliencePolicies.cs
│   └── ResilienceExtensions.cs
└── CommonSharedExtensions.cs
```

### Project Configuration

```xml
<!-- Common.Shared/Common.Shared.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>DigitalInspectionSystem.Common.Shared</PackageId>
    <Version>1.0.0</Version>
    <Authors>Digital Inspection Team</Authors>
    <Description>Shared infrastructure components for Digital Inspection System microservices</Description>
    <PackageProjectUrl>https://github.com/yourorg/digital-inspection-system</PackageProjectUrl>
    <RepositoryUrl>https://github.com/yourorg/digital-inspection-system</RepositoryUrl>
  </PropertyGroup>
</Project>
```

### Usage in Microservices

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Register Common.Shared services
    services.AddCommonSharedServices(configuration);
    
    // Register microservice-specific services
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
    
    services.AddScoped<IInspectionRepository, InspectionRepository>();
    
    return services;
}
```

### Versioning Strategy

**Semantic Versioning**: MAJOR.MINOR.PATCH

- **MAJOR**: Breaking changes (e.g., interface changes, removed methods)
- **MINOR**: New features, backward compatible (e.g., new methods, new classes)
- **PATCH**: Bug fixes, backward compatible

**Example**:
- `1.0.0` - Initial release
- `1.1.0` - Add new `IDistributedCacheService.ExistsAsync` method
- `1.1.1` - Fix bug in `RedisCacheService.GetAsync`
- `2.0.0` - Change `IMessagePublisher.PublishAsync` signature (breaking)

### Publishing Process

```bash
# Build and pack
cd src/Common.Shared
dotnet pack -c Release

# Push to Azure Artifacts
dotnet nuget push bin/Release/DigitalInspectionSystem.Common.Shared.1.0.0.nupkg \
  --source "DigitalInspectionSystem" \
  --api-key <api-key>
```

### Consuming in Microservices

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="DigitalInspectionSystem.Common.Shared" Version="1.0.0" />
</ItemGroup>

<!-- InspectionService.Infrastructure/InspectionService.Infrastructure.csproj -->
<ItemGroup>
  <PackageReference Include="DigitalInspectionSystem.Common.Shared" />
</ItemGroup>
```

### Testing Common.Shared

```csharp
// Common.Shared.Tests/Caching/RedisCacheServiceTests.cs
public class RedisCacheServiceTests
{
    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnValue()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        var cache = new RedisCacheService(redis, logger);
        
        // Act
        var result = await cache.GetAsync<string>("key");
        
        // Assert
        result.Should().NotBeNull();
    }
}
```

## Migration Path

### Phase 1: Create Common.Shared
1. Create Common.Shared project
2. Move generic infrastructure code from Inspection Service
3. Write comprehensive tests
4. Publish to Azure Artifacts

### Phase 2: Update Inspection Service
1. Add Common.Shared NuGet reference
2. Remove duplicated code
3. Update DI registration to use Common.Shared
4. Verify tests pass

### Phase 3: Apply to Other Services
1. Update each microservice to use Common.Shared
2. Remove duplicated code
3. Verify consistency across services

## References

- [NuGet Package Versioning](https://docs.microsoft.com/en-us/nuget/concepts/package-versioning)
- [Azure Artifacts](https://azure.microsoft.com/en-us/services/devops/artifacts/)
- [Semantic Versioning](https://semver.org/)

## Related ADRs

- [ADR 0001: Clean Architecture](0001-clean-architecture.md)
- [ADR 0002: CQRS with MediatR](0002-cqrs-with-mediatr.md)
