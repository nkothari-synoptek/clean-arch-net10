# Implementation Plan: Digital Inspection System

## Overview

This implementation plan creates a .NET 10 microservices architecture with Clean Architecture principles. The plan focuses on establishing the foundational structure, Common.Shared NuGet package, a complete sample microservice (Inspection Service) with CRUD operations, API Gateway with authentication, and deployment infrastructure. The implementation follows a module-based folder structure with comprehensive observability and testing.

## Tasks

- [x] 1. Set up solution structure and Central Package Management
  - Create solution file and folder structure
  - Create Directory.Packages.props with all package versions
  - Enable ManagePackageVersionsCentrally and CentralPackageTransitivePinningEnabled
  - Create .editorconfig for consistent code style
  - _Requirements: 24.1, 24.2, 24.3, 24.5_

- [x] 2. Create Common.Shared private NuGet package
  - [x] 2.1 Create Common.Shared class library project
    - Create project with .NET 8.0 target framework
    - Configure for NuGet package generation
    - Add package metadata (version, authors, description)
    - _Requirements: 18.1_

  - [x] 2.2 Implement generic Redis caching service
    - Create IDistributedCacheService interface
    - Implement RedisCacheService with StackExchange.Redis
    - Create CacheExtensions for DI registration
    - _Requirements: 18.3_

  - [x] 2.3 Implement generic Azure Service Bus abstractions
    - Create IMessagePublisher and IMessageConsumer interfaces
    - Implement ServiceBusPublisher and ServiceBusConsumer
    - Create ServiceBusExtensions for DI registration
    - _Requirements: 18.2_

  - [x] 2.4 Implement HTTP resilience policies
    - Create HttpResiliencePolicies with Polly (retry, circuit breaker, timeout)
    - Create ResilienceExtensions for IHttpClientBuilder
    - _Requirements: 5.3_

  - [x] 2.5 Implement high-performance logging delegates
    - Create LogMessages class with LoggerMessage.Define patterns
    - Create common logging delegates for infrastructure operations
    - Create LoggingExtensions for configuration
    - _Requirements: 22.1, 22.2, 22.3, 22.5_

  - [x] 2.6 Implement OpenTelemetry configuration
    - Create OpenTelemetryExtensions for tracing and metrics
    - Create ActivitySourceProvider for distributed tracing
    - Create MetricsProvider for custom metrics
    - Configure Azure Monitor exporters
    - _Requirements: 21.1, 21.2, 21.3, 21.4, 21.6_

  - [x] 2.7 Implement service-to-service authentication
    - Create ServiceAuthenticationHandler for OAuth 2.0 client credentials
    - Create ServiceAuthenticationExtensions for DI registration
    - Implement token acquisition using Azure.Identity
    - _Requirements: 23.1, 23.2, 23.3, 23.6_

  - [x] 2.8 Create CommonSharedExtensions for unified registration
    - Create AddCommonSharedServices extension method
    - Wire up all Common.Shared services
    - _Requirements: 18.1_

  - [x] 2.9 Write unit tests for Common.Shared components
    - Test Redis cache service operations
    - Test Service Bus publisher
    - Test HTTP resilience policies
    - _Requirements: 12.4, 12.5_

- [x] 3. Checkpoint - Verify Common.Shared package builds
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Create Inspection Service microservice structure
  - [x] 4.1 Create seven projects for Inspection Service
    - Create InspectionService.Domain class library
    - Create InspectionService.Application class library
    - Create InspectionService.Infrastructure class library
    - Create InspectionService.Api web API project
    - Create InspectionService.Shared.Kernel class library
    - Create five test projects (Domain.Tests, Application.Tests, Infrastructure.Tests, Api.Tests, ArchitectureTests)
    - _Requirements: 1.1, 20.1, 20.2, 20.3, 20.4, 20.5_

  - [x] 4.2 Configure project references
    - Application references Domain and Common.Shared
    - Infrastructure references Application and Common.Shared
    - Api references Domain, Application, Infrastructure, and Common.Shared
    - Configure test projects to reference their corresponding projects
    - _Requirements: 1.3, 1.4, 1.5_

  - [x] 4.3 Write architecture tests with NetArchTest
    - Test Domain has zero external dependencies
    - Test Application only references Domain
    - Test Infrastructure only references Application
    - Test dependency flow is unidirectional inward
    - _Requirements: 2.1, 2.2, 2.3, 2.5_

- [x] 5. Implement Domain layer for Inspections module
  - [x] 5.1 Create Shared.Kernel base classes
    - Create Entity base class with Id and domain events
    - Create ValueObject base class with equality
    - Create Result<T> for error handling
    - Create Guard class for validation
    - _Requirements: 1.2_

  - [x] 5.2 Create Inspection entity and value objects
    - Create Inspection entity with factory method
    - Create InspectionStatus value object
    - Create InspectionItem entity
    - Implement Complete() domain method with business rules
    - _Requirements: 19.1_

  - [x] 5.3 Create domain events
    - Create InspectionCompletedEvent
    - Create InspectionCreatedEvent
    - _Requirements: 19.1_

  - [x] 5.4 Write unit tests for Inspection entity
    - Test Inspection.Create factory method
    - Test Complete() with valid and invalid states
    - Test business rule enforcement
    - _Requirements: 12.1_

- [x] 6. Implement Application layer for Inspections module
  - [x] 6.1 Create CQRS infrastructure
    - Configure MediatR in Application project
    - Create ValidationBehavior for FluentValidation
    - Create LoggingBehavior for request logging
    - _Requirements: 3.1, 3.4, 3.5_

  - [x] 6.2 Implement Create Inspection command (CRUD - Create)
    - Create CreateInspectionCommand with DTOs
    - Create CreateInspectionCommandValidator with FluentValidation
    - Create CreateInspectionCommandHandler
    - Implement logging, metrics, and tracing in handler
    - _Requirements: 25.1, 19.2_

  - [x] 6.3 Implement Get Inspection query (CRUD - Read single)
    - Create GetInspectionByIdQuery
    - Create GetInspectionByIdQueryHandler with caching
    - Create InspectionDto for response
    - _Requirements: 25.2, 19.2_

  - [x] 6.4 Implement List Inspections query (CRUD - Read list)
    - Create ListInspectionsQuery with pagination
    - Create ListInspectionsQueryHandler
    - Create InspectionSummaryDto and PagedResult<T>
    - _Requirements: 25.2, 19.2_

  - [x] 6.5 Implement Update Inspection command (CRUD - Update)
    - Create UpdateInspectionCommand
    - Create UpdateInspectionCommandValidator
    - Create UpdateInspectionCommandHandler with cache invalidation
    - _Requirements: 25.3, 19.2_

  - [x] 6.6 Implement Delete Inspection command (CRUD - Delete)
    - Create DeleteInspectionCommand
    - Create DeleteInspectionCommandHandler with cache invalidation
    - _Requirements: 25.4, 19.2_

  - [x] 6.7 Define repository and service interfaces
    - Create IInspectionRepository interface
    - Create ICacheService interface (if not using Common.Shared directly)
    - _Requirements: 4.1, 4.3_

  - [x] 6.8 Write unit tests for command and query handlers
    - Test CreateInspectionCommandHandler with mocked repository
    - Test GetInspectionByIdQueryHandler with mocked cache and repository
    - Test UpdateInspectionCommandHandler
    - Test DeleteInspectionCommandHandler
    - Use NSubstitute for mocking
    - _Requirements: 12.2_

  - [x] 6.9 Write property test for MediatR command handling
    - **Property 3: MediatR Processes All Commands and Queries**
    - **Validates: Requirements 3.2, 3.3**
    - Test that all commands and queries are processed by handlers
    - _Requirements: 3.2, 3.3_

- [x] 7. Implement Infrastructure layer for Inspections module
  - [x] 7.1 Create ApplicationDbContext and entity configurations
    - Create ApplicationDbContext with DbSet<Inspection>
    - Create InspectionConfiguration using Fluent API
    - Create InspectionItemConfiguration
    - Configure value object mappings
    - _Requirements: 11.1, 11.2, 11.3, 19.3_

  - [x] 7.2 Implement InspectionRepository
    - Implement IInspectionRepository
    - Use Common.Shared IDistributedCacheService for caching
    - Implement GetByIdAsync with cache-aside pattern
    - Implement GetPagedAsync for list queries
    - Implement AddAsync, UpdateAsync, DeleteAsync
    - _Requirements: 4.2, 8.2, 8.3, 19.3_

  - [x] 7.3 Create database migrations
    - Generate initial migration for Inspection tables
    - Verify migration scripts
    - _Requirements: 11.4_

  - [x] 7.4 Implement InspectionEventPublisher
    - Use Common.Shared IMessagePublisher
    - Implement PublishInspectionCompletedAsync
    - Implement PublishInspectionCreatedAsync
    - _Requirements: 7.2_

  - [x] 7.5 Create DependencyInjection.cs
    - Register Common.Shared services
    - Register DbContext with connection string
    - Register repositories
    - Configure HTTP clients with resilience policies
    - _Requirements: 4.5, 13.1_

  - [x] 7.6 Write integration tests for InspectionRepository
    - Use Testcontainers for PostgreSQL
    - Test GetByIdAsync with database
    - Test cache-aside pattern
    - Test CRUD operations
    - _Requirements: 12.3_

  - [x] 7.7 Write property test for cache-aside pattern
    - **Property 9: Cache-Aside Pattern for Frequently Accessed Data**
    - **Validates: Requirements 8.2, 8.3**
    - Test that cache is checked first, then database on miss
    - _Requirements: 8.2, 8.3_

- [x] 8. Implement API layer for Inspections module
  - [x] 8.1 Create InspectionsController with CRUD endpoints
    - Implement POST /api/inspections (Create)
    - Implement GET /api/inspections/{id} (Read single)
    - Implement GET /api/inspections (Read list)
    - Implement PUT /api/inspections/{id} (Update)
    - Implement DELETE /api/inspections/{id} (Delete)
    - Add authorization policies to endpoints
    - _Requirements: 25.1, 25.2, 25.3, 25.4, 19.4_

  - [x] 8.2 Configure Program.cs
    - Register all services (Domain, Application, Infrastructure)
    - Configure Serilog with structured logging
    - Configure OpenTelemetry with Azure Monitor
    - Configure health checks
    - Configure Swagger/OpenAPI
    - Load configuration from appsettings.json
    - _Requirements: 10.1, 10.5, 13.2, 13.3, 13.4, 21.6_

  - [x] 8.3 Create ExceptionHandlingMiddleware
    - Handle ValidationException with 400 response
    - Handle domain exceptions with 400 response
    - Handle unhandled exceptions with 500 response
    - Log all exceptions with structured logging
    - _Requirements: 10.2_

  - [x] 8.4 Configure appsettings.json
    - Add connection strings (database, Redis, Service Bus)
    - Add Azure Entra ID configuration
    - Add OpenTelemetry configuration
    - Add Serilog configuration
    - _Requirements: 11.5, 13.4_

  - [x] 8.5 Write API integration tests
    - Use WebApplicationFactory
    - Test Create endpoint returns 201
    - Test Get endpoint returns 200
    - Test Update endpoint returns 204
    - Test Delete endpoint returns 204
    - Test validation errors return 400
    - _Requirements: 12.3_

- [x] 9. Checkpoint - Verify Inspection Service works end-to-end
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Create API Gateway with YARP
  - [x] 10.1 Create ApiGateway web API project 
    - Create new ASP.NET Core web API project
    - Add YARP.ReverseProxy package
    - Add Microsoft.Identity.Web package
    - _Requirements: 9.1, 16.1_

  - [x] 10.2 Configure Azure Entra External authentication
    - Configure JWT Bearer authentication
    - Set Authority and Audience from configuration
    - Configure token validation parameters
    - _Requirements: 16.2, 16.6_

  - [x] 10.3 Configure authorization policies
    - Create CanCreateInspection policy
    - Create CanCompleteInspection policy
    - Create CanViewAllInspections policy
    - _Requirements: 9.3, 16.5_

  - [x] 10.4 Configure YARP routing
    - Create ReverseProxy configuration in appsettings.json
    - Define routes for Inspection Service
    - Configure cluster destinations
    - _Requirements: 9.4, 9.6_

  - [x] 10.5 Configure rate limiting
    - Add rate limiting middleware
    - Configure per-user rate limits
    - _Requirements: 9.5_

  - [x] 10.6 Configure request logging
    - Add Serilog request logging
    - Log request/response details
    - _Requirements: 9.5, 10.2_

  - [x] 10.7 Write property test for authentication
    - **Property 10: Unauthenticated Requests Are Rejected**
    - **Validates: Requirements 9.2, 16.2**
    - Test that requests without valid JWT are rejected with 401
    - _Requirements: 9.2, 16.2_

  - [x] 10.8 Write property test for authorization
    - **Property 11: Authenticated Requests Are Authorized**
    - **Validates: Requirements 9.3, 16.5**
    - Test that authorization policies are evaluated
    - _Requirements: 9.3, 16.5_

- [x] 11. Create Kubernetes deployment manifests
  - [x] 11.1 Create Deployment manifest for Inspection Service
    - Define container spec with image
    - Configure environment variables
    - Configure resource requests and limits
    - Configure liveness and readiness probes
    - _Requirements: 17.2, 17.5, 17.6_

  - [x] 11.2 Create Service manifest for Inspection Service
    - Define ClusterIP service type
    - Configure port mappings
    - _Requirements: 17.4_

  - [x] 11.3 Create HorizontalPodAutoscaler for Inspection Service
    - Configure min/max replicas
    - Configure CPU and memory targets
    - _Requirements: 17.7_

  - [x] 11.4 Create Deployment manifest for API Gateway
    - Define container spec with image
    - Configure environment variables
    - Configure resource requests and limits
    - Configure health probes
    - _Requirements: 17.2, 17.5, 17.6_

  - [x] 11.5 Create Service and Ingress for API Gateway
    - Define LoadBalancer service type for external access
    - Create Ingress resource with TLS
    - _Requirements: 17.3, 9.8_

  - [x] 11.6 Write property test for Kubernetes configuration
    - **Property 18: Each Microservice Has Kubernetes Manifests**
    - **Validates: Requirements 17.2**
    - Test that all microservices have Deployment, Service, and HPA manifests
    - _Requirements: 17.2_

  - [x] 11.7 Write property test for internal service configuration
    - **Property 19: Internal Microservices Use ClusterIP Service Type**
    - **Validates: Requirements 17.4**
    - Test that non-gateway services use ClusterIP
    - _Requirements: 17.4_

- [x] 12. Create project scaffolding script
  - [x] 12.1 Create PowerShell script for new microservices
    - Accept service name as parameter
    - Create seven projects with correct structure
    - Add project references
    - Add Common.Shared NuGet reference
    - Add common NuGet packages
    - Create module-based folder structure
    - _Requirements: 14.1, 14.3, 19.1, 19.2, 19.3, 19.4_

  - [x] 12.2 Create template files for common patterns
    - Create entity template
    - Create command/query handler template
    - Create repository template
    - Create controller template
    - _Requirements: 14.3_

  - [x] 12.3 Create configuration templates
    - Create appsettings.json template
    - Create Program.cs template
    - Create DependencyInjection.cs template
    - _Requirements: 14.4_

- [x] 13. Create documentation
  - [x] 13.1 Create architecture documentation
    - Document Clean Architecture structure
    - Document dependency rules
    - Document module-based organization
    - _Requirements: 15.1_

  - [x] 13.2 Create developer onboarding guide
    - Document how to create a new microservice
    - Document how to add a new feature
    - Document how to run tests
    - Document how to deploy
    - _Requirements: 15.2_

  - [x] 13.3 Create architecture decision records
    - Document decision to use Clean Architecture
    - Document decision to use CQRS with MediatR
    - Document decision to use Common.Shared NuGet
    - Document decision to use module-based structure
    - _Requirements: 15.4_

  - [x] 13.4 Create architectural compliance checklist
    - Checklist for dependency rules
    - Checklist for testing requirements
    - Checklist for observability
    - Checklist for security
    - _Requirements: 15.5_

- [x] 14. Final checkpoint - Verify complete system
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties with minimum 100 iterations
- Unit tests validate specific examples and edge cases
- The implementation creates a complete, production-ready foundation that can be replicated for additional microservices
- Common.Shared package should be published to a private NuGet feed (Azure Artifacts or similar)
- All microservices follow the same structure and patterns established in the Inspection Service
