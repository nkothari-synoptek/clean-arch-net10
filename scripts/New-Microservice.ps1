<#
.SYNOPSIS
    Creates a new microservice with Clean Architecture structure.

.DESCRIPTION
    This script scaffolds a new microservice following the Digital Inspection System architecture.
    It creates seven projects (Domain, Application, Infrastructure, Api, Shared.Kernel, and five test projects)
    with proper references, folder structure, and Common.Shared NuGet package references.

.PARAMETER ServiceName
    The name of the microservice (e.g., "Reporting", "MasterData").
    Will be used to create projects like ReportingService.Domain, ReportingService.Application, etc.

.PARAMETER ModuleName
    The name of the initial module/feature to create (e.g., "Reports", "Products").
    This creates the initial folder structure in each layer.

.EXAMPLE
    .\New-Microservice.ps1 -ServiceName "Reporting" -ModuleName "Reports"
    Creates a new Reporting microservice with a Reports module.

.EXAMPLE
    .\New-Microservice.ps1 -ServiceName "MasterData" -ModuleName "Products"
    Creates a new MasterData microservice with a Products module.
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$ServiceName,
    
    [Parameter(Mandatory=$true)]
    [string]$ModuleName
)

$ErrorActionPreference = "Stop"

# Validate service name
if ($ServiceName -notmatch '^[A-Z][a-zA-Z0-9]*$') {
    Write-Error "ServiceName must start with an uppercase letter and contain only alphanumeric characters."
    exit 1
}

# Validate module name
if ($ModuleName -notmatch '^[A-Z][a-zA-Z0-9]*$') {
    Write-Error "ModuleName must start with an uppercase letter and contain only alphanumeric characters."
    exit 1
}

$ServiceFullName = "${ServiceName}Service"
$RootPath = Get-Location

Write-Host "Creating microservice: $ServiceFullName" -ForegroundColor Green
Write-Host "Initial module: $ModuleName" -ForegroundColor Green
Write-Host ""

# Define project paths
$SrcPath = Join-Path $RootPath "src"
$TestsPath = Join-Path $RootPath "tests"

$DomainProject = Join-Path $SrcPath "$ServiceFullName.Domain"
$ApplicationProject = Join-Path $SrcPath "$ServiceFullName.Application"
$InfrastructureProject = Join-Path $SrcPath "$ServiceFullName.Infrastructure"
$ApiProject = Join-Path $SrcPath "$ServiceFullName.Api"
$SharedKernelProject = Join-Path $SrcPath "$ServiceFullName.Shared.Kernel"

$DomainTestsProject = Join-Path $TestsPath "$ServiceFullName.Domain.Tests"
$ApplicationTestsProject = Join-Path $TestsPath "$ServiceFullName.Application.Tests"
$InfrastructureTestsProject = Join-Path $TestsPath "$ServiceFullName.Infrastructure.Tests"
$ApiTestsProject = Join-Path $TestsPath "$ServiceFullName.Api.Tests"
$ArchitectureTestsProject = Join-Path $TestsPath "$ServiceFullName.ArchitectureTests"

# Create directories
Write-Host "Creating project directories..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $SrcPath | Out-Null
New-Item -ItemType Directory -Force -Path $TestsPath | Out-Null

# Function to create a class library project
function New-ClassLibrary {
    param(
        [string]$ProjectPath,
        [string]$ProjectName
    )
    
    Write-Host "  Creating $ProjectName..." -ForegroundColor Yellow
    dotnet new classlib -n $ProjectName -o $ProjectPath --framework net10.0
    
    # Remove default Class1.cs
    $defaultFile = Join-Path $ProjectPath "Class1.cs"
    if (Test-Path $defaultFile) {
        Remove-Item $defaultFile
    }
}

# Function to create a web API project
function New-WebApi {
    param(
        [string]$ProjectPath,
        [string]$ProjectName
    )
    
    Write-Host "  Creating $ProjectName..." -ForegroundColor Yellow
    dotnet new webapi -n $ProjectName -o $ProjectPath --framework net10.0 --no-openapi
    
    # Remove default files
    $weatherForecastFile = Join-Path $ProjectPath "WeatherForecast.cs"
    $controllersPath = Join-Path $ProjectPath "Controllers"
    
    if (Test-Path $weatherForecastFile) {
        Remove-Item $weatherForecastFile
    }
    if (Test-Path $controllersPath) {
        Remove-Item -Recurse -Force $controllersPath
    }
}

# Function to create a test project
function New-TestProject {
    param(
        [string]$ProjectPath,
        [string]$ProjectName
    )
    
    Write-Host "  Creating $ProjectName..." -ForegroundColor Yellow
    dotnet new xunit -n $ProjectName -o $ProjectPath --framework net10.0
    
    # Remove default test file
    $defaultFile = Join-Path $ProjectPath "UnitTest1.cs"
    if (Test-Path $defaultFile) {
        Remove-Item $defaultFile
    }
}

# Create main projects
Write-Host "`nCreating main projects..." -ForegroundColor Cyan
New-ClassLibrary -ProjectPath $DomainProject -ProjectName "$ServiceFullName.Domain"
New-ClassLibrary -ProjectPath $ApplicationProject -ProjectName "$ServiceFullName.Application"
New-ClassLibrary -ProjectPath $InfrastructureProject -ProjectName "$ServiceFullName.Infrastructure"
New-WebApi -ProjectPath $ApiProject -ProjectName "$ServiceFullName.Api"
New-ClassLibrary -ProjectPath $SharedKernelProject -ProjectName "$ServiceFullName.Shared.Kernel"

# Create test projects
Write-Host "`nCreating test projects..." -ForegroundColor Cyan
New-TestProject -ProjectPath $DomainTestsProject -ProjectName "$ServiceFullName.Domain.Tests"
New-TestProject -ProjectPath $ApplicationTestsProject -ProjectName "$ServiceFullName.Application.Tests"
New-TestProject -ProjectPath $InfrastructureTestsProject -ProjectName "$ServiceFullName.Infrastructure.Tests"
New-TestProject -ProjectPath $ApiTestsProject -ProjectName "$ServiceFullName.Api.Tests"
New-TestProject -ProjectPath $ArchitectureTestsProject -ProjectName "$ServiceFullName.ArchitectureTests"

# Add project references
Write-Host "`nAdding project references..." -ForegroundColor Cyan

# Application references Domain
Write-Host "  Application -> Domain" -ForegroundColor Yellow
dotnet add "$ApplicationProject/$ServiceFullName.Application.csproj" reference "$DomainProject/$ServiceFullName.Domain.csproj"

# Infrastructure references Application
Write-Host "  Infrastructure -> Application" -ForegroundColor Yellow
dotnet add "$InfrastructureProject/$ServiceFullName.Infrastructure.csproj" reference "$ApplicationProject/$ServiceFullName.Application.csproj"

# Api references Domain, Application, Infrastructure
Write-Host "  Api -> Domain, Application, Infrastructure" -ForegroundColor Yellow
dotnet add "$ApiProject/$ServiceFullName.Api.csproj" reference "$DomainProject/$ServiceFullName.Domain.csproj"
dotnet add "$ApiProject/$ServiceFullName.Api.csproj" reference "$ApplicationProject/$ServiceFullName.Application.csproj"
dotnet add "$ApiProject/$ServiceFullName.Api.csproj" reference "$InfrastructureProject/$ServiceFullName.Infrastructure.csproj"

# Test project references
Write-Host "  Test projects -> Main projects" -ForegroundColor Yellow
dotnet add "$DomainTestsProject/$ServiceFullName.Domain.Tests.csproj" reference "$DomainProject/$ServiceFullName.Domain.csproj"
dotnet add "$ApplicationTestsProject/$ServiceFullName.Application.Tests.csproj" reference "$ApplicationProject/$ServiceFullName.Application.csproj"
dotnet add "$InfrastructureTestsProject/$ServiceFullName.Infrastructure.Tests.csproj" reference "$InfrastructureProject/$ServiceFullName.Infrastructure.csproj"
dotnet add "$ApiTestsProject/$ServiceFullName.Api.Tests.csproj" reference "$ApiProject/$ServiceFullName.Api.csproj"

# Architecture tests reference all main projects
dotnet add "$ArchitectureTestsProject/$ServiceFullName.ArchitectureTests.csproj" reference "$DomainProject/$ServiceFullName.Domain.csproj"
dotnet add "$ArchitectureTestsProject/$ServiceFullName.ArchitectureTests.csproj" reference "$ApplicationProject/$ServiceFullName.Application.csproj"
dotnet add "$ArchitectureTestsProject/$ServiceFullName.ArchitectureTests.csproj" reference "$InfrastructureProject/$ServiceFullName.Infrastructure.csproj"
dotnet add "$ArchitectureTestsProject/$ServiceFullName.ArchitectureTests.csproj" reference "$ApiProject/$ServiceFullName.Api.csproj"

# Add Common.Shared NuGet package reference
Write-Host "`nAdding Common.Shared package references..." -ForegroundColor Cyan
dotnet add "$ApplicationProject/$ServiceFullName.Application.csproj" package Common.Shared
dotnet add "$InfrastructureProject/$ServiceFullName.Infrastructure.csproj" package Common.Shared
dotnet add "$ApiProject/$ServiceFullName.Api.csproj" package Common.Shared

# Add common NuGet packages
Write-Host "`nAdding common NuGet packages..." -ForegroundColor Cyan

# Application layer packages
Write-Host "  Application layer packages..." -ForegroundColor Yellow
dotnet add "$ApplicationProject/$ServiceFullName.Application.csproj" package MediatR
dotnet add "$ApplicationProject/$ServiceFullName.Application.csproj" package FluentValidation
dotnet add "$ApplicationProject/$ServiceFullName.Application.csproj" package FluentValidation.DependencyInjectionExtensions

# Infrastructure layer packages
Write-Host "  Infrastructure layer packages..." -ForegroundColor Yellow
dotnet add "$InfrastructureProject/$ServiceFullName.Infrastructure.csproj" package Microsoft.EntityFrameworkCore
dotnet add "$InfrastructureProject/$ServiceFullName.Infrastructure.csproj" package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add "$InfrastructureProject/$ServiceFullName.Infrastructure.csproj" package Microsoft.EntityFrameworkCore.Design
dotnet add "$InfrastructureProject/$ServiceFullName.Infrastructure.csproj" package Azure.Identity
dotnet add "$InfrastructureProject/$ServiceFullName.Infrastructure.csproj" package Azure.Extensions.AspNetCore.Configuration.Secrets

# API layer packages
Write-Host "  API layer packages..." -ForegroundColor Yellow
dotnet add "$ApiProject/$ServiceFullName.Api.csproj" package Microsoft.AspNetCore.OpenApi
dotnet add "$ApiProject/$ServiceFullName.Api.csproj" package Swashbuckle.AspNetCore
dotnet add "$ApiProject/$ServiceFullName.Api.csproj" package Serilog.AspNetCore
dotnet add "$ApiProject/$ServiceFullName.Api.csproj" package Microsoft.Identity.Web
dotnet add "$ApiProject/$ServiceFullName.Api.csproj" package AspNetCore.HealthChecks.NpgSql
dotnet add "$ApiProject/$ServiceFullName.Api.csproj" package AspNetCore.HealthChecks.Redis
dotnet add "$ApiProject/$ServiceFullName.Api.csproj" package AspNetCore.HealthChecks.AzureServiceBus

# Test packages
Write-Host "  Test packages..." -ForegroundColor Yellow
$testProjects = @($DomainTestsProject, $ApplicationTestsProject, $InfrastructureTestsProject, $ApiTestsProject, $ArchitectureTestsProject)
foreach ($testProject in $testProjects) {
    $projectName = Split-Path $testProject -Leaf
    dotnet add "$testProject/$projectName.csproj" package FluentAssertions
    dotnet add "$testProject/$projectName.csproj" package NSubstitute
    dotnet add "$testProject/$projectName.csproj" package FsCheck
    dotnet add "$testProject/$projectName.csproj" package FsCheck.Xunit
}

# Architecture tests specific packages
dotnet add "$ArchitectureTestsProject/$ServiceFullName.ArchitectureTests.csproj" package NetArchTest.Rules

# Infrastructure tests specific packages
dotnet add "$InfrastructureTestsProject/$ServiceFullName.Infrastructure.Tests.csproj" package Testcontainers
dotnet add "$InfrastructureTestsProject/$ServiceFullName.Infrastructure.Tests.csproj" package Testcontainers.PostgreSql

# API tests specific packages
dotnet add "$ApiTestsProject/$ServiceFullName.Api.Tests.csproj" package Microsoft.AspNetCore.Mvc.Testing

Write-Host "`nCreating module-based folder structure..." -ForegroundColor Cyan

# Create Domain module structure
$domainModulePath = Join-Path $DomainProject $ModuleName
New-Item -ItemType Directory -Force -Path (Join-Path $domainModulePath "Entities") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $domainModulePath "ValueObjects") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $domainModulePath "Events") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $domainModulePath "Services") | Out-Null
Write-Host "  Created Domain/$ModuleName structure" -ForegroundColor Yellow

# Create Application module structure
$appModulePath = Join-Path $ApplicationProject $ModuleName
New-Item -ItemType Directory -Force -Path (Join-Path $appModulePath "Commands") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $appModulePath "Queries") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $appModulePath "DTOs") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $appModulePath "Interfaces") | Out-Null
Write-Host "  Created Application/$ModuleName structure" -ForegroundColor Yellow

# Create Application Common structure
$appCommonPath = Join-Path $ApplicationProject "Common"
New-Item -ItemType Directory -Force -Path (Join-Path $appCommonPath "Behaviors") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $appCommonPath "Models") | Out-Null
Write-Host "  Created Application/Common structure" -ForegroundColor Yellow

# Create Infrastructure module structure
$infraModulePath = Join-Path $InfrastructureProject "Persistence"
New-Item -ItemType Directory -Force -Path (Join-Path $infraModulePath "Configurations") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $infraModulePath "Repositories" $ModuleName) | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $infraModulePath "Migrations") | Out-Null
Write-Host "  Created Infrastructure/Persistence structure" -ForegroundColor Yellow

$infraMessagingPath = Join-Path $InfrastructureProject "Messaging"
New-Item -ItemType Directory -Force -Path $infraMessagingPath | Out-Null
Write-Host "  Created Infrastructure/Messaging structure" -ForegroundColor Yellow

$infraConfigPath = Join-Path $InfrastructureProject "Configuration"
New-Item -ItemType Directory -Force -Path $infraConfigPath | Out-Null
Write-Host "  Created Infrastructure/Configuration structure" -ForegroundColor Yellow

# Create API module structure
$apiModulePath = Join-Path $ApiProject "Controllers"
New-Item -ItemType Directory -Force -Path (Join-Path $apiModulePath $ModuleName) | Out-Null
Write-Host "  Created Api/Controllers/$ModuleName structure" -ForegroundColor Yellow

$apiMiddlewarePath = Join-Path $ApiProject "Middleware"
New-Item -ItemType Directory -Force -Path $apiMiddlewarePath | Out-Null
Write-Host "  Created Api/Middleware structure" -ForegroundColor Yellow

# Create Shared.Kernel structure
$sharedKernelBasePath = Join-Path $SharedKernelProject "Base"
New-Item -ItemType Directory -Force -Path $sharedKernelBasePath | Out-Null
$sharedKernelCommonPath = Join-Path $SharedKernelProject "Common"
New-Item -ItemType Directory -Force -Path $sharedKernelCommonPath | Out-Null
Write-Host "  Created Shared.Kernel structure" -ForegroundColor Yellow

Write-Host "`nMicroservice scaffolding completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Copy template files from scripts/templates/ to your new projects"
Write-Host "  2. Update appsettings.json with your configuration"
Write-Host "  3. Implement your domain entities in $ServiceFullName.Domain/$ModuleName/Entities"
Write-Host "  4. Implement CQRS handlers in $ServiceFullName.Application/$ModuleName"
Write-Host "  5. Implement repositories in $ServiceFullName.Infrastructure/Persistence/Repositories/$ModuleName"
Write-Host "  6. Implement controllers in $ServiceFullName.Api/Controllers/$ModuleName"
Write-Host ""
Write-Host "Project structure created at:" -ForegroundColor Cyan
Write-Host "  src/$ServiceFullName.Domain"
Write-Host "  src/$ServiceFullName.Application"
Write-Host "  src/$ServiceFullName.Infrastructure"
Write-Host "  src/$ServiceFullName.Api"
Write-Host "  src/$ServiceFullName.Shared.Kernel"
Write-Host "  tests/$ServiceFullName.Domain.Tests"
Write-Host "  tests/$ServiceFullName.Application.Tests"
Write-Host "  tests/$ServiceFullName.Infrastructure.Tests"
Write-Host "  tests/$ServiceFullName.Api.Tests"
Write-Host "  tests/$ServiceFullName.ArchitectureTests"
