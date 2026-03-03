# ADR 0004: Module-Based Folder Organization

## Status

Accepted

## Context

In Clean Architecture, code is organized into layers (Domain, Application, Infrastructure, API). Within each layer, we need to decide how to organize code. We have two main options:

### Option 1: Technical Organization
Organize by technical concern (entities, repositories, controllers):
```
Domain/
├── Entities/
│   ├── Inspection.cs
│   ├── Inspector.cs
│   └── Report.cs
├── ValueObjects/
│   ├── InspectionStatus.cs
│   └── InspectorCertification.cs
└── Services/
    ├── InspectionDomainService.cs
    └── InspectorDomainService.cs
```

### Option 2: Module/Feature Organization
Organize by business module/feature:
```
Domain/
├── Inspections/
│   ├── Entities/
│   │   └── Inspection.cs
│   ├── ValueObjects/
│   │   └── InspectionStatus.cs
│   └── Services/
│       └── InspectionDomainService.cs
└── Inspectors/
    ├── Entities/
    │   └── Inspector.cs
    └── ValueObjects/
        └── InspectorCertification.cs
```

We need to choose an organization strategy that:
1. Makes related code easy to find
2. Supports team ownership of features
3. Scales as the codebase grows
4. Minimizes merge conflicts
5. Makes feature boundaries explicit

## Decision

We will organize code by module/feature (business capability) rather than by technical concern.

### Structure

Each layer will have top-level folders for modules, with technical subfolders within:

**Domain Layer**:
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
│   └── Events/
│       └── InspectionCompletedEvent.cs
├── Inspectors/                     # Inspector module
│   ├── Entities/
│   │   └── Inspector.cs
│   └── ValueObjects/
│       └── InspectorCertification.cs
└── Common/                         # Shared domain primitives
    ├── Entity.cs
    └── ValueObject.cs
```

**Application Layer**:
```
Application/
├── Inspections/                    # Inspection module
│   ├── Commands/
│   │   ├── CreateInspection/
│   │   │   ├── CreateInspectionCommand.cs
│   │   │   ├── CreateInspectionCommandHandler.cs
│   │   │   └── CreateInspectionCommandValidator.cs
│   │   └── UpdateInspection/
│   │       ├── UpdateInspectionCommand.cs
│   │       ├── UpdateInspectionCommandHandler.cs
│   │       └── UpdateInspectionCommandValidator.cs
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
    ├── Behaviors/
    └── Mappings/
```

**Infrastructure Layer**:
```
Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/
│   │   ├── InspectionConfiguration.cs
│   │   └── InspectorConfiguration.cs
│   └── Repositories/
│       ├── Inspections/
│       │   └── InspectionRepository.cs
│       └── Inspectors/
│           └── InspectorRepository.cs
├── ExternalServices/
│   ├── NotificationServiceAdapter.cs
│   └── MasterDataServiceAdapter.cs
└── Messaging/
    ├── InspectionEventPublisher.cs
    └── InspectorEventConsumer.cs
```

**API Layer**:
```
Api/
├── Controllers/
│   ├── Inspections/
│   │   └── InspectionsController.cs
│   └── Inspectors/
│       └── InspectorsController.cs
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
└── Program.cs
```

### Module Definition

A module represents a business capability or bounded context:
- **Inspections**: Everything related to creating, managing, and completing inspections
- **Inspectors**: Everything related to inspector management and certification
- **Reports**: Everything related to report generation and distribution

Modules are cohesive units that:
- Have clear boundaries
- Can be understood independently
- Can be owned by a team
- Can evolve independently (within architectural constraints)

## Consequences

### Positive

1. **Cohesion**: Related code is located together
   - All inspection-related code is in `Inspections/` folders
   - Easy to find everything related to a feature
   - Changes to a feature are localized

2. **Discoverability**: New developers can find code quickly
   - "Where is the code for creating inspections?" → `Application/Inspections/Commands/CreateInspection/`
   - "Where is the Inspection entity?" → `Domain/Inspections/Entities/Inspection.cs`
   - Within 2 minutes, developers can locate feature implementation points

3. **Team Ownership**: Teams can own specific modules
   - Team A owns Inspections module
   - Team B owns Inspectors module
   - Clear boundaries reduce coordination overhead

4. **Scalability**: Structure scales as features grow
   - Adding a new feature = adding a new module folder
   - Modules don't interfere with each other
   - Codebase can grow without becoming unwieldy

5. **Merge Conflicts**: Reduced conflicts between teams
   - Teams work in different module folders
   - Less chance of conflicting changes
   - Easier to review PRs (changes are localized)

6. **Feature Boundaries**: Explicit boundaries between features
   - Clear what belongs to each module
   - Easier to identify cross-module dependencies
   - Can extract modules to separate services if needed

7. **Vertical Slicing**: Easy to see complete feature implementation
   - Follow a feature from Domain → Application → Infrastructure → API
   - All in the same module folder structure
   - Good for understanding and documentation

### Negative

1. **Duplication**: Some technical patterns may be duplicated across modules
   - Mitigation: Use Common.Shared for infrastructure patterns
   - Mitigation: Use Common/ folders for shared domain/application code
   - Mitigation: Accept some duplication for cohesion

2. **Navigation**: More folders to navigate
   - Mitigation: Modern IDEs have good search and navigation
   - Mitigation: Consistent structure makes navigation predictable
   - Mitigation: Benefits of cohesion outweigh navigation cost

3. **Shared Code**: Need Common/ folders for shared code
   - Mitigation: Keep Common/ folders minimal
   - Mitigation: Only put truly shared code in Common/
   - Mitigation: Prefer duplication over premature abstraction

4. **Learning Curve**: Developers need to understand module boundaries
   - Mitigation: Document module definitions
   - Mitigation: Code reviews enforce boundaries
   - Mitigation: Architecture tests can verify module isolation

## Alternatives Considered

### 1. Technical Organization

**Structure**: Organize by technical concern (entities, repositories, controllers)

```
Domain/
├── Entities/
│   ├── Inspection.cs
│   ├── Inspector.cs
│   └── Report.cs
├── ValueObjects/
└── Services/
```

**Rejected because**:
- Related code is scattered across folders
- Difficult to find all code for a feature
- Changes to a feature touch many folders
- No clear feature boundaries
- Doesn't scale well as codebase grows

### 2. Hybrid Organization

**Structure**: Mix of technical and module organization

```
Domain/
├── Entities/
│   ├── Inspections/
│   │   └── Inspection.cs
│   └── Inspectors/
│       └── Inspector.cs
└── ValueObjects/
    ├── Inspections/
    └── Inspectors/
```

**Rejected because**:
- Inconsistent structure is confusing
- Still requires navigating multiple folders for a feature
- Doesn't provide clear benefits over pure module organization

### 3. Flat Structure

**Structure**: All files in layer root with prefixes

```
Domain/
├── InspectionEntity.cs
├── InspectionStatus.cs
├── InspectionDomainService.cs
├── InspectorEntity.cs
└── InspectorCertification.cs
```

**Rejected because**:
- Doesn't scale (too many files in one folder)
- No clear organization
- Difficult to navigate
- No feature boundaries

## Implementation Notes

### Adding a New Module

1. **Create module folder in Domain layer**:
```bash
mkdir -p src/InspectionService.Domain/Reports/Entities
mkdir -p src/InspectionService.Domain/Reports/ValueObjects
mkdir -p src/InspectionService.Domain/Reports/Services
mkdir -p src/InspectionService.Domain/Reports/Events
```

2. **Create module folder in Application layer**:
```bash
mkdir -p src/InspectionService.Application/Reports/Commands
mkdir -p src/InspectionService.Application/Reports/Queries
mkdir -p src/InspectionService.Application/Reports/DTOs
mkdir -p src/InspectionService.Application/Reports/Interfaces
```

3. **Create module folder in Infrastructure layer**:
```bash
mkdir -p src/InspectionService.Infrastructure/Persistence/Repositories/Reports
mkdir -p src/InspectionService.Infrastructure/Messaging
```

4. **Create module folder in API layer**:
```bash
mkdir -p src/InspectionService.Api/Controllers/Reports
```

### Naming Conventions

- **Module folders**: PascalCase, plural (e.g., `Inspections/`, `Inspectors/`)
- **Technical subfolders**: PascalCase, plural (e.g., `Entities/`, `Commands/`, `Queries/`)
- **Files**: PascalCase, singular (e.g., `Inspection.cs`, `CreateInspectionCommand.cs`)

### Cross-Module Dependencies

**Rule**: Modules should be loosely coupled. Avoid direct dependencies between modules.

**Good**: Module A publishes domain event, Module B subscribes
```csharp
// Inspections module publishes event
AddDomainEvent(new InspectionCompletedEvent(Id));

// Reports module subscribes to event
public class InspectionCompletedEventHandler : INotificationHandler<InspectionCompletedEvent>
{
    // Generate report when inspection is completed
}
```

**Bad**: Module A directly calls Module B
```csharp
// Avoid this
var inspector = _inspectorRepository.GetByIdAsync(inspectorId);
```

**If cross-module data is needed**: Use queries or read models
```csharp
// Inspections module queries Inspector data
var inspector = await _mediator.Send(new GetInspectorByIdQuery(inspectorId));
```

### Common Folders

Use `Common/` folders for code that is truly shared across modules:

- **Domain/Common/**: Base classes (Entity, ValueObject), shared interfaces
- **Application/Common/**: Behaviors, shared interfaces, common DTOs
- **Infrastructure/Common/**: Shared infrastructure code (if any)

**Rule**: Only put code in Common/ if it's used by multiple modules. Prefer duplication over premature abstraction.

## Module Examples

### Inspections Module
**Responsibility**: Creating, managing, and completing inspections

**Domain**:
- `Inspection` entity
- `InspectionItem` entity
- `InspectionStatus` value object
- `InspectionCompletedEvent` domain event

**Application**:
- `CreateInspectionCommand`
- `UpdateInspectionCommand`
- `CompleteInspectionCommand`
- `GetInspectionByIdQuery`
- `ListInspectionsQuery`

### Inspectors Module
**Responsibility**: Managing inspectors and their certifications

**Domain**:
- `Inspector` entity
- `InspectorCertification` value object
- `InspectorCertifiedEvent` domain event

**Application**:
- `RegisterInspectorCommand`
- `CertifyInspectorCommand`
- `GetInspectorByIdQuery`
- `ListInspectorsQuery`

### Reports Module
**Responsibility**: Generating and distributing reports

**Domain**:
- `Report` entity
- `ReportFormat` value object
- `ReportGeneratedEvent` domain event

**Application**:
- `GenerateReportCommand`
- `GetReportByIdQuery`
- `ListReportsQuery`

## References

- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Modular Monolith by Kamil Grzybek](https://www.kamilgrzybek.com/design/modular-monolith-primer/)
- [Vertical Slice Architecture by Jimmy Bogard](https://jimmybogard.com/vertical-slice-architecture/)

## Related ADRs

- [ADR 0001: Clean Architecture](0001-clean-architecture.md)
- [ADR 0002: CQRS with MediatR](0002-cqrs-with-mediatr.md)
