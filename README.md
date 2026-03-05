# Digital Inspection System

A .NET 10.0 microservices architecture built with Clean Architecture principles.

## Solution Structure

```
DigitalInspectionSystem/
├── src/                          # Source code
│   ├── Common.Shared/           # Private NuGet package for shared components
│   ├── ApiGateway/              # YARP API Gateway with authentication
│   └── Services/                # Microservices
│       └── InspectionService/   # Sample microservice
├── tests/                       # Test projects
├── docs/                        # Documentation
├── Directory.Packages.props     # Central Package Management
├── .editorconfig               # Code style configuration
└── DigitalInspectionSystem.sln # Solution file
```

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Docker Desktop (for local development with Redis, PostgreSQL, etc.)
- Azure CLI (for deployment)

### Building the Solution

```bash
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test
```

## Architecture

This solution follows Clean Architecture principles with:

- **Domain Layer**: Pure business logic with zero external dependencies
- **Application Layer**: Use cases and CQRS handlers using MediatR
- **Infrastructure Layer**: EF Core, Redis, HTTP clients, Azure Service Bus
- **API Layer**: ASP.NET Core web host and composition root

## Central Package Management

This solution uses Central Package Management with `Directory.Packages.props` to ensure consistent package versions across all projects.

When adding a package reference, omit the version:

```xml
<PackageReference Include="MediatR" />
```

Package versions are managed centrally in `Directory.Packages.props`.

## Code Style

The solution uses `.editorconfig` for consistent code style. Key conventions:

- 4 spaces for indentation
- Private fields prefixed with underscore (`_fieldName`)
- Interfaces prefixed with `I` (`IRepository`)
- PascalCase for types and public members
- Braces required for all code blocks

## Documentation

See the `docs/` folder for:

- Architecture documentation
- Developer onboarding guide
- Architecture decision records
- Deployment guides
