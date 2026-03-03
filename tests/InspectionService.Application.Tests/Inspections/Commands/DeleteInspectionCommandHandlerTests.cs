using Common.Shared.Caching;
using FluentAssertions;
using InspectionService.Application.Inspections.Commands.DeleteInspection;
using InspectionService.Application.Inspections.Interfaces;
using InspectionService.Domain.Inspections.Entities;
using InspectionService.Shared.Kernel.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InspectionService.Application.Tests.Inspections.Commands;

public class DeleteInspectionCommandHandlerTests
{
    private readonly IInspectionRepository _repository;
    private readonly IDistributedCacheService _cache;
    private readonly ILogger<DeleteInspectionCommandHandler> _logger;
    private readonly DeleteInspectionCommandHandler _handler;

    public DeleteInspectionCommandHandlerTests()
    {
        _repository = Substitute.For<IInspectionRepository>();
        _cache = Substitute.For<IDistributedCacheService>();
        _logger = Substitute.For<ILogger<DeleteInspectionCommandHandler>>();
        _handler = new DeleteInspectionCommandHandler(_repository, _cache, _logger);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesInspectionAndInvalidatesCache()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Test Inspection",
            "Test Description",
            "creator@example.com");

        var command = new DeleteInspectionCommand(inspectionId);
        var cacheKey = $"inspection:{inspectionId}";

        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(inspection)));

        _repository.DeleteAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _repository.Received(1).DeleteAsync(inspectionId, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(cacheKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InspectionNotFound_ReturnsFailure()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var command = new DeleteInspectionCommand(inspectionId);

        var errorMessage = $"Inspection with ID {inspectionId} not found";
        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<Inspection>(errorMessage)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);

        await _repository.DidNotReceive().DeleteAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());

        await _cache.DidNotReceive().RemoveAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeleteFails_ReturnsFailureAndDoesNotInvalidateCache()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Test Inspection",
            "Test Description",
            "creator@example.com");

        var command = new DeleteInspectionCommand(inspectionId);

        var errorMessage = "Database delete failed";
        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(inspection)));

        _repository.DeleteAsync(inspectionId, Arg.Any<CancellationToken>())
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
        var command = new DeleteInspectionCommand(inspectionId);

        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to delete inspection");
    }

    [Fact]
    public async Task Handle_CacheInvalidationFails_ReturnsFailure()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Test Inspection",
            "Test Description",
            "creator@example.com");

        var command = new DeleteInspectionCommand(inspectionId);
        var cacheKey = $"inspection:{inspectionId}";

        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(inspection)));

        _repository.DeleteAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _cache.RemoveAsync(cacheKey, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cache error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to delete inspection");
    }

    [Fact]
    public async Task Handle_ValidDelete_VerifiesInspectionExistsFirst()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Test Inspection",
            "Test Description",
            "creator@example.com");

        var command = new DeleteInspectionCommand(inspectionId);

        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(inspection)));

        _repository.DeleteAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify that GetByIdAsync was called before DeleteAsync
        Received.InOrder(() =>
        {
            _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>());
            _repository.DeleteAsync(inspectionId, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_MultipleDeleteAttempts_EachVerifiesExistence()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Test Inspection",
            "Test Description",
            "creator@example.com");

        var command = new DeleteInspectionCommand(inspectionId);

        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(inspection)));

        _repository.DeleteAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result1 = await _handler.Handle(command, CancellationToken.None);
        var result2 = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        await _repository.Received(2).GetByIdAsync(inspectionId, Arg.Any<CancellationToken>());
        await _repository.Received(2).DeleteAsync(inspectionId, Arg.Any<CancellationToken>());
    }
}
