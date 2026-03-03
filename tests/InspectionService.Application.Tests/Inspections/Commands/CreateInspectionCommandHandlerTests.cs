using FluentAssertions;
using InspectionService.Application.Inspections.Commands.CreateInspection;
using InspectionService.Application.Inspections.Interfaces;
using InspectionService.Domain.Inspections.Entities;
using InspectionService.Shared.Kernel.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InspectionService.Application.Tests.Inspections.Commands;

public class CreateInspectionCommandHandlerTests
{
    private readonly IInspectionRepository _repository;
    private readonly ILogger<CreateInspectionCommandHandler> _logger;
    private readonly CreateInspectionCommandHandler _handler;

    public CreateInspectionCommandHandlerTests()
    {
        _repository = Substitute.For<IInspectionRepository>();
        _logger = Substitute.For<ILogger<CreateInspectionCommandHandler>>();
        _handler = new CreateInspectionCommandHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithInspectionId()
    {
        // Arrange
        var command = new CreateInspectionCommand
        {
            Title = "Safety Inspection",
            Description = "Monthly safety inspection",
            CreatedBy = "john.doe@example.com",
            Items = new List<CreateInspectionItemDto>
            {
                new() { Name = "Fire Extinguisher", Description = "Check expiry date", Order = 1 },
                new() { Name = "Emergency Exit", Description = "Verify accessibility", Order = 2 }
            }
        };

        _repository.AddAsync(Arg.Any<Inspection>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(Result.Success(Guid.NewGuid())));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(
            Arg.Is<Inspection>(i => 
                i.Title == command.Title && 
                i.Description == command.Description &&
                i.CreatedBy == command.CreatedBy &&
                i.Items.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommandWithNoItems_ReturnsSuccessWithInspectionId()
    {
        // Arrange
        var command = new CreateInspectionCommand
        {
            Title = "Empty Inspection",
            Description = "Inspection without items",
            CreatedBy = "jane.doe@example.com",
            Items = new List<CreateInspectionItemDto>()
        };

        _repository.AddAsync(Arg.Any<Inspection>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(Result.Success(Guid.NewGuid())));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(
            Arg.Is<Inspection>(i => i.Items.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RepositoryFailure_ReturnsFailure()
    {
        // Arrange
        var command = new CreateInspectionCommand
        {
            Title = "Test Inspection",
            Description = "Test description",
            CreatedBy = "test@example.com",
            Items = new List<CreateInspectionItemDto>()
        };

        var errorMessage = "Database connection failed";
        _repository.AddAsync(Arg.Any<Inspection>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(Result.Failure<Guid>(errorMessage)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_ReturnsFailure()
    {
        // Arrange
        var command = new CreateInspectionCommand
        {
            Title = "Test Inspection",
            Description = "Test description",
            CreatedBy = "test@example.com",
            Items = new List<CreateInspectionItemDto>()
        };

        _repository.AddAsync(Arg.Any<Inspection>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to create inspection");
    }

    [Fact]
    public async Task Handle_MultipleItems_AddsAllItemsToInspection()
    {
        // Arrange
        var command = new CreateInspectionCommand
        {
            Title = "Comprehensive Inspection",
            Description = "Full facility inspection",
            CreatedBy = "inspector@example.com",
            Items = new List<CreateInspectionItemDto>
            {
                new() { Name = "Item 1", Description = "Description 1", Order = 1 },
                new() { Name = "Item 2", Description = "Description 2", Order = 2 },
                new() { Name = "Item 3", Description = "Description 3", Order = 3 }
            }
        };

        _repository.AddAsync(Arg.Any<Inspection>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(Result.Success(Guid.NewGuid())));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<Inspection>(i => i.Items.Count == 3),
            Arg.Any<CancellationToken>());
    }
}
