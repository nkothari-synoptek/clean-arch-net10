using FluentAssertions;
using NetArchTest.Rules;

namespace InspectionService.ArchitectureTests;

public class ArchitectureTests
{
    private const string DomainNamespace = "InspectionService.Domain";
    private const string ApplicationNamespace = "InspectionService.Application";
    private const string InfrastructureNamespace = "InspectionService.Infrastructure";
    private const string ApiNamespace = "InspectionService.Api";
    private const string SharedKernelNamespace = "InspectionService.Shared.Kernel";

    [Fact]
    public void Domain_Should_Not_HaveAnyExternalDependencies()
    {
        // Arrange
        var domainAssembly = typeof(InspectionService.Domain.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain layer should have zero external dependencies except optionally Shared.Kernel and Common.Shared");
    }

    [Fact]
    public void Domain_Should_OnlyDependOn_SharedKernel_And_CommonShared()
    {
        // Arrange
        var domainAssembly = typeof(InspectionService.Domain.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "MediatR",
                "StackExchange.Redis",
                "Azure.Messaging.ServiceBus")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain layer should not reference infrastructure packages");
    }

    [Fact]
    public void Application_Should_OnlyReference_Domain()
    {
        // Arrange
        var applicationAssembly = typeof(InspectionService.Application.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                InfrastructureNamespace,
                ApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Application layer should only reference Domain project and Common.Shared");
    }

    [Fact]
    public void Application_Should_NotReference_InfrastructurePackages()
    {
        // Arrange
        var applicationAssembly = typeof(InspectionService.Application.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "StackExchange.Redis",
                "Azure.Messaging.ServiceBus")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Application layer should not reference infrastructure packages");
    }

    [Fact]
    public void Infrastructure_Should_OnlyReference_Application()
    {
        // Arrange
        var infrastructureAssembly = typeof(InspectionService.Infrastructure.AssemblyReference).Assembly;

        // Act
        var result = Types.InAssembly(infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Infrastructure layer should only reference Application project and Common.Shared");
    }

    [Fact]
    public void Dependencies_Should_Flow_Inward()
    {
        // Arrange & Act
        var domainAssembly = typeof(InspectionService.Domain.AssemblyReference).Assembly;
        var applicationAssembly = typeof(InspectionService.Application.AssemblyReference).Assembly;
        var infrastructureAssembly = typeof(InspectionService.Infrastructure.AssemblyReference).Assembly;

        // Domain should not depend on Application or Infrastructure
        var domainResult = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        // Application should not depend on Infrastructure or Api
        var applicationResult = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        // Infrastructure should not depend on Api
        var infrastructureResult = Types.InAssembly(infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        // Assert
        domainResult.IsSuccessful.Should().BeTrue(
            "Domain layer should not depend on outer layers");
        applicationResult.IsSuccessful.Should().BeTrue(
            "Application layer should not depend on Infrastructure or Api layers");
        infrastructureResult.IsSuccessful.Should().BeTrue(
            "Infrastructure layer should not depend on Api layer");
    }

}
