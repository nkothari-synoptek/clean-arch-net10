using Common.Shared.Caching;
using FluentAssertions;
using InspectionService.Application.Inspections.Commands.UpdateInspection;
using InspectionService.Application.Inspections.Interfaces;
using InspectionService.Domain.Inspections.Entities;
using InspectionService.Shared.Kernel.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InspectionService.Application.Tests.Inspections.Commands;

public class UpdateInspectionCommandHandlerTests
{
    private readonly IInspectionRepository _repository;
    private readonly IDistributedCacheService _cache;
    private readonly ILogger<UpdateInspectionCommandHandler> _logger;
    private readonly UpdateInspectionCommandHandler _handler;

    public UpdateInspectionCommandHandlerTests()
    {
        _repository = Substitute.For<IInspectionRepository>();
        _cache = Substitute.For<IDistributedCacheService>();
        _logger = Substitute.For<ILogger<UpdateInspectionCommandHandler>>();
        _handler = new UpdateInspectionCommandHandler(_repository, _cache, _logger);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesInspectionAndInvalidatesCache()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Original Title",
            "Original Description",
            "creator@example.com");

        var command = new UpdateInspectionCommand
        {
            Id = inspectionId,
            Title = "Updated Title",
            Description = "Updated Description"
        };

        var cacheKey = $"inspection:{inspectionId}";

        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(inspection)));

        _repository.UpdateAsync(Arg.Any<Inspection>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inspection.Title.Should().Be("Updated Title");
        inspection.Description.Should().Be("Updated Description");

        await _repository.Received(1).UpdateAsync(
            Arg.Is<Inspection>(i => i.Title == "Updated Title" && i.Description == "Updated Description"),
            Arg.Any<CancellationToken>());

        await _cache.Received(1).RemoveAsync(cacheKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InspectionNotFound_ReturnsFailure()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var command = new UpdateInspectionCommand
        {
            Id = inspectionId,
            Title = "Updated Title",
            Description = "Updated Description"
        };

        var errorMessage = $"Inspection with ID {inspectionId} not found";
        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<Inspection>(errorMessage)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);

        await _repository.DidNotReceive().UpdateAsync(
            Arg.Any<Inspection>(),
            Arg.Any<CancellationToken>());

        await _cache.DidNotReceive().RemoveAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateFails_ReturnsFailureAndDoesNotInvalidateCache()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Original Title",
            "Original Description",
            "creator@example.com");

        var command = new UpdateInspectionCommand
        {
            Id = inspectionId,
            Title = "Updated Title",
            Description = "Updated Description"
        };

        var errorMessage = "Database update failed";
        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(inspection)));

        _repository.UpdateAsync(Arg.Any<Inspection>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(errorMessage)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);

        await _cache.DidNotReceive().RemoveAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_ReturnsFailure()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var command = new UpdateInspectionCommand
        {
            Id = inspectionId,
            Title = "Updated Title",
            Description = "Updated Description"
        };

        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to update inspection");
    }

    [Fact]
    public async Task Handle_CacheInvalidationFails_ReturnsFailure()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Original Title",
            "Original Description",
            "creator@example.com");

        var command = new UpdateInspectionCommand
        {
            Id = inspectionId,
            Title = "Updated Title",
            Description = "Updated Description"
        };

        var cacheKey = $"inspection:{inspectionId}";

        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(inspection)));

        _repository.UpdateAsync(Arg.Any<Inspection>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _cache.RemoveAsync(cacheKey, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cache error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to update inspection");
    }

    [Fact]
    public async Task Handle_ValidUpdate_PreservesInspectionId()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Original Title",
            "Original Description",
            "creator@example.com");

        var command = new UpdateInspectionCommand
        {
            Id = inspectionId,
            Title = "New Title",
            Description = "New Description"
        };

        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(inspection)));

        _repository.UpdateAsync(Arg.Any<Inspection>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inspection.Id.Should().NotBeEmpty();
        await _repository.Received(1).UpdateAsync(
            Arg.Is<Inspection>(i => i.Id == inspection.Id),
            Arg.Any<CancellationToken>());
    }
}
