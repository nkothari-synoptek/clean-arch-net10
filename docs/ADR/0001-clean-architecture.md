# ADR 0001: Adopt Clean Architecture

## Status

Accepted

## Context

We need to build a digital inspection system that is maintainable, testable, and scalable. The system will evolve over time with new features, changing requirements, and potentially different infrastructure technologies. We need an architectural approach that:

1. Separates business logic from infrastructure concerns
2. Makes the codebase easy to understand and navigate
3. Enables independent testing of different layers
4. Allows infrastructure changes without affecting business logic
5. Supports multiple teams working on different features

## Decision

We will adopt Clean Architecture (also known as Onion Architecture or Hexagonal Architecture) for all microservices in the system.

### Structure

Each microservice will have four main layers:

1. **Domain Layer** (innermost)
   - Contains pure business logic
   - Zero external dependencies
   - Entities, value objects, domain services, domain events

2. **Application Layer**
   - Orchestrates use cases
   - Defines interfaces for external concerns
   - Commands, queries, handlers (CQRS pattern)
   - References Domain layer only

3. **Infrastructure Layer**
   - Implements external concerns
   - Database access, caching, HTTP clients, message bus
   - Implements interfaces defined in Application layer
   - References Application layer

4. **API Layer** (outermost)
   - Web host and composition root
   - Controllers, middleware, configuration
   - References all other layers
   - Thin layer that wires everything together

### Dependency Rule

Dependencies point inward: `Api → Infrastructure → Application → Domain`

Inner layers have no knowledge of outer layers.

### Project Structure

Each microservice will have seven projects:
- `{Service}.Domain` - Business logic
- `{Service}.Application` - Use cases
- `{Service}.Infrastructure` - External concerns
- `{Service}.Api` - Web host
- `{Service}.Shared.Kernel` - Cross-cutting primitives (optional)
- Five test projects (Domain.Tests, Application.Tests, Infrastructure.Tests, Api.Tests, ArchitectureTests)

## Consequences

### Positive

1. **Testability**: Each layer can be tested independently with appropriate strategies
   - Domain: Pure unit tests with zero dependencies
   - Application: Unit tests with mocked repositories
   - Infrastructure: Integration tests with real databases
   - API: Integration tests with WebApplicationFactory

2. **Maintainability**: Clear separation of concerns makes code easy to understand and modify
   - Business logic is isolated in Domain layer
   - Use cases are explicit in Application layer
   - Infrastructure details are hidden behind interfaces

3. **Flexibility**: Infrastructure can be changed without affecting business logic
   - Swap database providers (SQL Server → PostgreSQL)
   - Change caching providers (Redis → Memcached)
   - Replace external service integrations

4. **Team Scalability**: Multiple teams can work on different layers or features independently
   - Domain experts focus on Domain layer
   - Infrastructure specialists work on Infrastructure layer
   - Frontend developers interact with API layer

5. **Architectural Compliance**: NetArchTest can automatically verify dependency rules
   - Catch violations in CI/CD pipeline
   - Prevent accidental coupling

### Negative

1. **Initial Complexity**: More projects and files than a simple layered architecture
   - Mitigation: Provide scaffolding scripts to generate structure
   - Mitigation: Comprehensive documentation and examples

2. **Learning Curve**: Developers need to understand Clean Architecture principles
   - Mitigation: Developer onboarding guide
   - Mitigation: Code reviews to ensure compliance
   - Mitigation: Architecture tests to catch violations

3. **Boilerplate Code**: More interfaces and abstractions
   - Mitigation: Use templates for common patterns
   - Mitigation: Leverage Common.Shared for reusable components

4. **Over-Engineering Risk**: May be overkill for very simple features
   - Mitigation: Follow YAGNI (You Aren't Gonna Need It) principle
   - Mitigation: Start simple, add complexity when needed

## Alternatives Considered

### 1. Traditional N-Tier Architecture

**Structure**: Presentation → Business Logic → Data Access

**Rejected because**:
- Business logic often leaks into presentation and data layers
- Difficult to test business logic in isolation
- Database-centric design makes it hard to change persistence technology
- Tight coupling between layers

### 2. Modular Monolith

**Structure**: Single application with feature modules

**Rejected because**:
- We need microservices for independent deployment and scaling
- Doesn't provide the same level of isolation as Clean Architecture
- Still need Clean Architecture within each module for maintainability

### 3. Vertical Slice Architecture

**Structure**: Features organized as vertical slices through all layers

**Rejected because**:
- Less emphasis on dependency rules
- Can lead to duplication across slices
- Harder to enforce architectural boundaries
- We still want clear layer separation for testability

## Implementation Notes

1. **Enforce with Architecture Tests**: Use NetArchTest to write tests that verify dependency rules
2. **Scaffolding Scripts**: Provide PowerShell scripts to generate new microservices with correct structure
3. **Code Reviews**: Ensure all code follows Clean Architecture principles
4. **Documentation**: Maintain comprehensive architecture documentation
5. **Examples**: Inspection Service serves as the reference implementation

## References

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Onion Architecture by Jeffrey Palermo](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/)
- [Hexagonal Architecture by Alistair Cockburn](https://alistair.cockburn.us/hexagonal-architecture/)
- [NetArchTest](https://github.com/BenMorris/NetArchTest)

## Related ADRs

- [ADR 0002: CQRS with MediatR](0002-cqrs-with-mediatr.md)
- [ADR 0003: Common.Shared NuGet Package](0003-common-shared-nuget.md)
- [ADR 0004: Module-Based Organization](0004-module-based-organization.md)
