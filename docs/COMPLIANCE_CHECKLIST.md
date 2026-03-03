# Architectural Compliance Checklist

This checklist helps ensure that microservices follow the architectural principles and patterns defined for the Digital Inspection System. Use this checklist during code reviews, architecture reviews, and when onboarding new services.

## Table of Contents

1. [Clean Architecture Compliance](#clean-architecture-compliance)
2. [Dependency Rules](#dependency-rules)
3. [CQRS Pattern Compliance](#cqrs-pattern-compliance)
4. [Module Organization](#module-organization)
5. [Testing Requirements](#testing-requirements)
6. [Observability Requirements](#observability-requirements)
7. [Security Requirements](#security-requirements)
8. [Common.Shared Usage](#commonshared-usage)
9. [Performance Requirements](#performance-requirements)
10. [Documentation Requirements](#documentation-requirements)

---

## Clean Architecture Compliance

### Project Structure

- [ ] Microservice has exactly seven projects:
  - [ ] `{Service}.Domain` - Business logic
  - [ ] `{Service}.Application` - Use cases
  - [ ] `{Service}.Infrastructure` - External concerns
  - [ ] `{Service}.Api` - Web host
  - [ ] `{Service}.Shared.Kernel` - Cross-cutting primitives (optional)
  - [ ] `{Service}.Domain.Tests` - Domain unit tests
  - [ ] `{Service}.Application.Tests` - Application unit tests
  - [ ] `{Service}.Infrastructure.Tests` - Infrastructure integration tests
  - [ ] `{Service}.Api.Tests` - API integration tests
  - [ ] `{Service}.ArchitectureTests` - Architecture compliance tests

### Layer Responsibilities

- [ ] **Domain Layer**:
  - [ ] Contains only business logic (entities, value objects, domain services)
  - [ ] Has zero external dependencies (except Shared.Kernel and Common.Shared base classes)
  - [ ] No infrastructure concerns (no database, HTTP, file I/O)
  - [ ] Uses factory methods for entity creation
  - [ ] Raises domain events for side effects

- [ ] **Application Layer**:
  - [ ] Defines use cases as commands and queries
  - [ ] Defines interfaces for external concerns (repositories, caching, messaging)
  - [ ] References only Domain layer and Common.Shared
  - [ ] Uses MediatR for CQRS pattern
  - [ ] Uses FluentValidation for input validation
  - [ ] Returns DTOs, not domain entities

- [ ] **Infrastructure Layer**:
  - [ ] Implements interfaces defined in Application layer
  - [ ] Contains EF Core DbContext and entity configurations
  - [ ] Implements repositories with caching
  - [ ] Implements external service adapters
  - [ ] Uses Common.Shared for infrastructure abstractions
  - [ ] References Application layer (and transitively Domain)

- [ ] **API Layer**:
  - [ ] Thin controllers that delegate to MediatR
  - [ ] Program.cs is the composition root
  - [ ] Configures all middleware
  - [ ] Loads configuration from appsettings.json
  - [ ] References all other layers

---

## Dependency Rules

### Automated Verification

- [ ] Architecture tests exist in `{Service}.ArchitectureTests` project
- [ ] Architecture tests use NetArchTest
- [ ] Architecture tests run as part of CI/CD pipeline

### Dependency Flow

- [ ] Domain layer has zero external project references (except Shared.Kernel)
- [ ] Application layer references only Domain layer
- [ ] Infrastructure layer references only Application layer
- [ ] API layer references Domain, Application, and Infrastructure layers
- [ ] Dependencies point inward: `Api → Infrastructure → Application → Domain`

### Test Verification

Run architecture tests to verify:
```bash
dotnet test tests/{Service}.ArchitectureTests
```

Expected tests:
- [ ] `Domain_Should_Not_HaveDependencyOnOtherProjects`
- [ ] `Application_Should_Not_HaveDependencyOnInfrastructure`
- [ ] `Application_Should_Not_HaveDependencyOnApi`
- [ ] `Infrastructure_Should_Not_HaveDependencyOnApi`

---

## CQRS Pattern Compliance

### Commands

- [ ] Commands represent actions that change state
- [ ] Commands are named with verb + entity (e.g., `CreateInspectionCommand`)
- [ ] Commands implement `IRequest<Result<T>>`
- [ ] Each command has a dedicated handler
- [ ] Each command has a validator (if validation is needed)
- [ ] Handlers are in `Application/{Module}/Commands/{CommandName}/` folder

### Queries

- [ ] Queries represent data retrieval requests
- [ ] Queries are named with Get/List + entity (e.g., `GetInspectionByIdQuery`)
- [ ] Queries implement `IRequest<Result<T>>`
- [ ] Each query has a dedicated handler
- [ ] Handlers are in `Application/{Module}/Queries/{QueryName}/` folder
- [ ] Queries return DTOs, not domain entities

### Handlers

- [ ] Each handler has a single responsibility
- [ ] Handlers use constructor injection for dependencies
- [ ] Handlers return `Result<T>` for error handling
- [ ] Handlers log operations using high-performance logging
- [ ] Handlers use repositories through interfaces

### Behaviors

- [ ] `ValidationBehavior` is registered for all commands
- [ ] `LoggingBehavior` is registered for all requests
- [ ] Behaviors are in `Application/Common/Behaviors/` folder

### Controllers

- [ ] Controllers are thin and delegate to MediatR
- [ ] Controllers use `IMediator` interface
- [ ] Controllers return appropriate HTTP status codes
- [ ] Controllers handle `Result<T>` failures with proper responses

---

## Module Organization

### Folder Structure

- [ ] Code is organized by module/feature, not by technical concern
- [ ] Each module has clear boundaries
- [ ] Module folders exist in all layers:
  - [ ] `Domain/{Module}/`
  - [ ] `Application/{Module}/`
  - [ ] `Infrastructure/Persistence/Repositories/{Module}/`
  - [ ] `Api/Controllers/{Module}/`

### Module Definition

- [ ] Each module represents a business capability
- [ ] Modules are cohesive (related code is together)
- [ ] Modules are loosely coupled (minimal dependencies)
- [ ] Cross-module communication uses domain events or queries

### Common Folders

- [ ] `Common/` folders contain only truly shared code
- [ ] Shared code is used by multiple modules
- [ ] Duplication is preferred over premature abstraction

---

## Testing Requirements

### Test Projects

- [ ] All five test projects exist and are configured
- [ ] Test projects reference appropriate testing packages:
  - [ ] xUnit
  - [ ] FluentAssertions
  - [ ] NSubstitute (for mocking)
  - [ ] Testcontainers (for integration tests)

### Domain Tests

- [ ] Domain entities have unit tests
- [ ] Factory methods are tested
- [ ] Business rules are tested
- [ ] Domain services are tested
- [ ] Tests have zero external dependencies
- [ ] Tests use FluentAssertions for assertions

### Application Tests

- [ ] Command handlers have unit tests
- [ ] Query handlers have unit tests
- [ ] Validators have unit tests
- [ ] Tests use mocked repositories (NSubstitute)
- [ ] Tests verify Result<T> success and failure cases

### Infrastructure Tests

- [ ] Repository implementations have integration tests
- [ ] Tests use Testcontainers for real databases
- [ ] Cache-aside pattern is tested
- [ ] CRUD operations are tested
- [ ] Tests clean up after themselves

### API Tests

- [ ] Controllers have integration tests
- [ ] Tests use WebApplicationFactory
- [ ] HTTP status codes are verified
- [ ] Request/response payloads are verified
- [ ] Authentication and authorization are tested

### Architecture Tests

- [ ] Dependency rules are verified
- [ ] Tests fail if architectural violations occur
- [ ] Tests run in CI/CD pipeline

### Test Coverage

- [ ] Domain layer has >80% code coverage
- [ ] Application layer has >80% code coverage
- [ ] Infrastructure layer has >60% code coverage
- [ ] API layer has >60% code coverage

---

## Observability Requirements

### Logging

- [ ] Serilog is configured in Program.cs
- [ ] Structured logging is used throughout
- [ ] High-performance logging delegates are used (LoggerMessage.Define)
- [ ] Log levels are appropriate:
  - [ ] Debug: Detailed diagnostic information
  - [ ] Information: General informational messages
  - [ ] Warning: Unexpected but recoverable situations
  - [ ] Error: Errors and exceptions
  - [ ] Fatal: Critical failures
- [ ] Sensitive data is not logged (PII, passwords, tokens)

### Distributed Tracing

- [ ] OpenTelemetry is configured in Program.cs
- [ ] ActivitySource is used for custom spans
- [ ] Trace IDs are included in logs
- [ ] HTTP requests are traced
- [ ] Database queries are traced
- [ ] External API calls are traced

### Metrics

- [ ] Custom metrics are defined for business operations
- [ ] Metrics use OpenTelemetry
- [ ] Metrics are exported to Azure Monitor
- [ ] Key metrics include:
  - [ ] Request count
  - [ ] Request duration
  - [ ] Error rate
  - [ ] Business operation counts

### Health Checks

- [ ] Health checks are configured in Program.cs
- [ ] Database health check is included
- [ ] Redis health check is included
- [ ] Service Bus health check is included
- [ ] Health endpoint is exposed: `/health`
- [ ] Liveness and readiness probes are configured in Kubernetes

---

## Security Requirements

### Authentication

- [ ] API Gateway handles authentication using Azure Entra External
- [ ] JWT tokens are validated
- [ ] Invalid tokens return 401 Unauthorized
- [ ] Internal microservices do not require authentication (behind gateway)

### Authorization

- [ ] Policy-based authorization is configured
- [ ] Controllers use `[Authorize]` attribute with policies
- [ ] Authorization policies are defined in Program.cs
- [ ] Unauthorized requests return 403 Forbidden

### Input Validation

- [ ] All commands have validators using FluentValidation
- [ ] Validation happens before handler execution
- [ ] Validation errors return 400 Bad Request
- [ ] Input is sanitized to prevent injection attacks

### Secrets Management

- [ ] Secrets are not hardcoded in source code
- [ ] Secrets are stored in Azure Key Vault or Kubernetes secrets
- [ ] Connection strings use environment variables
- [ ] API keys use environment variables

### HTTPS

- [ ] All external communication uses HTTPS
- [ ] TLS certificates are configured
- [ ] HTTP is redirected to HTTPS

### Service-to-Service Authentication

- [ ] Internal service calls use Azure Entra ID managed identities
- [ ] OAuth 2.0 client credentials flow is used
- [ ] Service tokens are validated
- [ ] Least privilege access is enforced

---

## Common.Shared Usage

### Package Reference

- [ ] Microservice references Common.Shared NuGet package
- [ ] Version is specified in Directory.Packages.props
- [ ] Package is restored successfully

### Service Registration

- [ ] `AddCommonSharedServices(configuration)` is called in Infrastructure DI
- [ ] Common.Shared services are registered before microservice-specific services

### Caching

- [ ] Repositories use `IDistributedCacheService` from Common.Shared
- [ ] Cache keys follow consistent pattern: `{entity}:{id}`
- [ ] Cache expiration is configured appropriately
- [ ] Cache invalidation happens on updates and deletes

### Service Bus

- [ ] Event publishers use `IMessagePublisher` from Common.Shared
- [ ] Event consumers use `IMessageConsumer` from Common.Shared
- [ ] Messages include correlation IDs for tracing

### HTTP Resilience

- [ ] HTTP clients use `AddCommonResiliencePolicies()` from Common.Shared
- [ ] Retry, circuit breaker, and timeout policies are applied
- [ ] Transient failures are handled gracefully

### Logging

- [ ] High-performance logging delegates from Common.Shared are used
- [ ] LoggerMessage.Define pattern is followed
- [ ] String interpolation is avoided in log statements

### OpenTelemetry

- [ ] OpenTelemetry configuration from Common.Shared is used
- [ ] ActivitySource and Metrics providers are used
- [ ] Telemetry is exported to Azure Monitor

---

## Performance Requirements

### Caching

- [ ] Frequently accessed data is cached
- [ ] Cache-aside pattern is implemented
- [ ] Cache expiration is configured
- [ ] Cache invalidation happens on updates

### Async/Await

- [ ] All I/O operations are async
- [ ] `async`/`await` is used correctly (no blocking calls)
- [ ] `ConfigureAwait(false)` is used in library code (if applicable)

### Database Queries

- [ ] EF Core queries are optimized
- [ ] Eager loading is used for related entities (`Include`)
- [ ] Projections are used for read-only queries (`Select`)
- [ ] Indexes are defined for frequently queried columns
- [ ] N+1 query problems are avoided

### Logging Performance

- [ ] LoggerMessage.Define is used for high-performance logging
- [ ] String interpolation is avoided in log statements
- [ ] Log levels are checked before expensive operations

### Resource Limits

- [ ] Kubernetes resource requests and limits are defined
- [ ] Memory limits are appropriate for workload
- [ ] CPU limits are appropriate for workload

---

## Documentation Requirements

### Code Documentation

- [ ] Public APIs have XML documentation comments
- [ ] Complex business logic has explanatory comments
- [ ] Domain events are documented
- [ ] Interfaces are documented

### README Files

- [ ] Microservice has a README.md in the root
- [ ] README explains what the service does
- [ ] README includes setup instructions
- [ ] README includes how to run tests
- [ ] README includes deployment instructions

### API Documentation

- [ ] Swagger/OpenAPI is configured
- [ ] API endpoints are documented
- [ ] Request/response models are documented
- [ ] Authentication requirements are documented

### Architecture Documentation

- [ ] Architecture decisions are documented in ADRs
- [ ] Module boundaries are documented
- [ ] Cross-module dependencies are documented

---

## Kubernetes Deployment

### Manifests

- [ ] Deployment manifest exists
- [ ] Service manifest exists
- [ ] HorizontalPodAutoscaler manifest exists
- [ ] ConfigMap manifest exists (if needed)
- [ ] Secrets manifest exists (if needed)

### Deployment Configuration

- [ ] Container image is specified
- [ ] Environment variables are configured
- [ ] Resource requests and limits are defined
- [ ] Liveness probe is configured
- [ ] Readiness probe is configured

### Service Configuration

- [ ] Service type is ClusterIP (for internal services)
- [ ] Service type is LoadBalancer (for API Gateway only)
- [ ] Port mappings are correct

### Autoscaling

- [ ] Min replicas is defined (e.g., 2)
- [ ] Max replicas is defined (e.g., 10)
- [ ] CPU target is defined (e.g., 70%)
- [ ] Memory target is defined (e.g., 80%)

---

## Central Package Management

### Directory.Packages.props

- [ ] Directory.Packages.props exists at solution root
- [ ] `ManagePackageVersionsCentrally` is enabled
- [ ] `CentralPackageTransitivePinningEnabled` is enabled
- [ ] All package versions are defined centrally

### Project Files

- [ ] Package references omit version attribute
- [ ] Versions are managed in Directory.Packages.props only
- [ ] No version conflicts exist

---

## Code Quality

### Code Style

- [ ] .editorconfig is configured
- [ ] Code follows consistent style
- [ ] Naming conventions are followed:
  - [ ] PascalCase for classes, methods, properties
  - [ ] camelCase for local variables, parameters
  - [ ] Interfaces start with `I`
  - [ ] Private fields start with `_`

### SOLID Principles

- [ ] Single Responsibility: Classes have one reason to change
- [ ] Open/Closed: Open for extension, closed for modification
- [ ] Liskov Substitution: Derived classes are substitutable
- [ ] Interface Segregation: Interfaces are focused
- [ ] Dependency Inversion: Depend on abstractions, not concretions

### Error Handling

- [ ] Result<T> pattern is used for business logic failures
- [ ] Exceptions are used for exceptional situations only
- [ ] Exceptions are logged with context
- [ ] Global exception handling middleware is configured

---

## Review Checklist Summary

Use this summary checklist during code reviews:

### Architecture
- [ ] Clean Architecture structure is followed
- [ ] Dependency rules are enforced
- [ ] Architecture tests pass

### CQRS
- [ ] Commands and queries are properly separated
- [ ] Handlers have single responsibility
- [ ] Controllers are thin

### Testing
- [ ] All test projects exist
- [ ] Tests cover critical paths
- [ ] Architecture tests verify compliance

### Observability
- [ ] Logging is configured
- [ ] Tracing is configured
- [ ] Metrics are defined
- [ ] Health checks are configured

### Security
- [ ] Authentication is configured
- [ ] Authorization is enforced
- [ ] Input is validated
- [ ] Secrets are not hardcoded

### Performance
- [ ] Caching is implemented
- [ ] Async/await is used
- [ ] Queries are optimized

### Documentation
- [ ] README exists
- [ ] API is documented
- [ ] Code is commented

---

## Automated Compliance Verification

### CI/CD Pipeline Checks

Add these checks to your CI/CD pipeline:

```yaml
# Example GitHub Actions workflow
- name: Run Architecture Tests
  run: dotnet test tests/{Service}.ArchitectureTests

- name: Run All Tests
  run: dotnet test --collect:"XPlat Code Coverage"

- name: Check Code Coverage
  run: |
    # Verify coverage thresholds
    # Domain: >80%, Application: >80%, Infrastructure: >60%, API: >60%

- name: Run Code Analysis
  run: dotnet build /p:TreatWarningsAsErrors=true

- name: Check for Secrets
  run: |
    # Use tools like truffleHog or git-secrets
    # Fail if secrets are detected
```

### Pre-Commit Hooks

Configure pre-commit hooks to catch issues early:

```bash
# .git/hooks/pre-commit
#!/bin/bash

# Run architecture tests
dotnet test tests/{Service}.ArchitectureTests

# Run code formatting
dotnet format --verify-no-changes

# Check for secrets
git diff --cached --name-only | xargs grep -i "password\|secret\|key" && exit 1

exit 0
```

---

## Getting Help

If you have questions about compliance:

1. Review the [Architecture Documentation](ARCHITECTURE.md)
2. Review the [Architecture Decision Records](ADR/)
3. Review the [Developer Onboarding Guide](DEVELOPER_ONBOARDING.md)
4. Ask in team chat or create an issue

---

## Continuous Improvement

This checklist is a living document. If you find:
- Missing items that should be checked
- Items that are no longer relevant
- Better ways to verify compliance

Please submit a pull request to update this checklist.

---

**Last Updated**: 2025-02-24
**Version**: 1.0.0
