using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InspectionService.Application.Inspections.Commands.CreateInspection;
using InspectionService.Application.Inspections.Commands.UpdateInspection;
using InspectionService.Application.Inspections.DTOs;
using InspectionService.Domain.Inspections.Entities;
using InspectionService.Domain.Inspections.ValueObjects;
using InspectionService.Shared.Kernel.Common;
using NSubstitute;

namespace InspectionService.Api.Tests.Controllers;

/// <summary>
/// Integration tests for InspectionsController
/// Validates: Requirements 12.3
/// </summary>
public class InspectionsControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InspectionsControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateInspection_WithValidData_Returns201Created()
    {
        // Arrange
        var command = new CreateInspectionCommand
        {
            Title = "Safety Inspection",
            Description = "Monthly safety inspection",
            CreatedBy = "test-user",
            Items = new List<CreateInspectionItemDto>
            {
                new()
                {
                    Name = "Fire Extinguisher Check",
                    Description = "Verify fire extinguisher is accessible",
                    Order = 1
                }
            }
        };

        _factory.MockRepository
            .AddAsync(Arg.Any<Inspection>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => 
            {
                var inspection = callInfo.Arg<Inspection>();
                return Task.FromResult(Result<Guid>.Success(inspection.Id));
            });

        // Act
        var response = await _client.PostAsJsonAsync("/api/inspections", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        
        var returnedId = await response.Content.ReadFromJsonAsync<Guid>();
        returnedId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetInspectionById_WithExistingId_Returns200Ok()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Safety Inspection",
            "Monthly safety inspection",
            "test-user");
        
        inspection.AddItem("Fire Extinguisher Check", "Verify fire extinguisher is accessible", 1);

        _factory.MockRepository
            .GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Inspection>.Success(inspection)));

        // Act
        var response = await _client.GetAsync($"/api/inspections/{inspectionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var dto = await response.Content.ReadFromJsonAsync<InspectionDto>();
        dto.Should().NotBeNull();
        dto!.Title.Should().Be("Safety Inspection");
        dto.Description.Should().Be("Monthly safety inspection");
        dto.CreatedBy.Should().Be("test-user");
        dto.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetInspectionById_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();

        _factory.MockRepository
            .GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<Inspection>($"Inspection with ID {inspectionId} not found")));

        // Act
        var response = await _client.GetAsync($"/api/inspections/{inspectionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateInspection_WithValidData_Returns204NoContent()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var command = new UpdateInspectionCommand
        {
            Id = inspectionId,
            Title = "Updated Safety Inspection",
            Description = "Updated monthly safety inspection"
        };

        var inspection = Inspection.Create(
            "Safety Inspection",
            "Monthly safety inspection",
            "test-user");
        
        inspection.AddItem("Fire Extinguisher Check", "Verify fire extinguisher is accessible", 1);

        _factory.MockRepository
            .GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Inspection>.Success(inspection)));

        _factory.MockRepository
            .UpdateAsync(Arg.Any<Inspection>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var response = await _client.PutAsJsonAsync($"/api/inspections/{inspectionId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateInspection_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var command = new UpdateInspectionCommand
        {
            Id = inspectionId,
            Title = "Updated Safety Inspection",
            Description = "Updated monthly safety inspection"
        };

        _factory.MockRepository
            .GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<Inspection>($"Inspection with ID {inspectionId} not found")));

        // Act
        var response = await _client.PutAsJsonAsync($"/api/inspections/{inspectionId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteInspection_WithExistingId_Returns204NoContent()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Safety Inspection",
            "Monthly safety inspection",
            "test-user");
        
        inspection.AddItem("Fire Extinguisher Check", "Verify fire extinguisher is accessible", 1);

        _factory.MockRepository
            .GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Inspection>.Success(inspection)));

        _factory.MockRepository
            .DeleteAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var response = await _client.DeleteAsync($"/api/inspections/{inspectionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteInspection_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();

        _factory.MockRepository
            .GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<Inspection>($"Inspection with ID {inspectionId} not found")));

        // Act
        var response = await _client.DeleteAsync($"/api/inspections/{inspectionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateInspection_WithEmptyTitle_Returns400BadRequest()
    {
        // Arrange
        var command = new CreateInspectionCommand
        {
            Title = "", // Invalid: empty title
            Description = "Monthly safety inspection",
            CreatedBy = "test-user",
            Items = new List<CreateInspectionItemDto>
            {
                new()
                {
                    Name = "Fire Extinguisher Check",
                    Description = "Verify fire extinguisher is accessible",
                    Order = 1
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/inspections", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateInspection_WithNoItems_Returns400BadRequest()
    {
        // Arrange
        var command = new CreateInspectionCommand
        {
            Title = "Safety Inspection",
            Description = "Monthly safety inspection",
            CreatedBy = "test-user",
            Items = new List<CreateInspectionItemDto>() // Invalid: no items
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/inspections", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateInspection_WithTitleTooLong_Returns400BadRequest()
    {
        // Arrange
        var command = new CreateInspectionCommand
        {
            Title = new string('A', 201), // Invalid: exceeds 200 characters
            Description = "Monthly safety inspection",
            CreatedBy = "test-user",
            Items = new List<CreateInspectionItemDto>
            {
                new()
                {
                    Name = "Fire Extinguisher Check",
                    Description = "Verify fire extinguisher is accessible",
                    Order = 1
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/inspections", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateInspection_WithMismatchedIds_Returns400BadRequest()
    {
        // Arrange
        var urlId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        
        var command = new UpdateInspectionCommand
        {
            Id = commandId, // Different from URL ID
            Title = "Updated Safety Inspection",
            Description = "Updated monthly safety inspection"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/inspections/{urlId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
