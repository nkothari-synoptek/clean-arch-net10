using Common.Shared.Caching;
using FluentAssertions;
using InspectionService.Application.Inspections.DTOs;
using InspectionService.Application.Inspections.Interfaces;
using InspectionService.Application.Inspections.Queries.GetInspectionById;
using InspectionService.Domain.Inspections.Entities;
using InspectionService.Shared.Kernel.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InspectionService.Application.Tests.Inspections.Queries;

public class GetInspectionByIdQueryHandlerTests
{
    private readonly IInspectionRepository _repository;
    private readonly IDistributedCacheService _cache;
    private readonly ILogger<GetInspectionByIdQueryHandler> _logger;
    private readonly GetInspectionByIdQueryHandler _handler;

    public GetInspectionByIdQueryHandlerTests()
    {
        _repository = Substitute.For<IInspectionRepository>();
        _cache = Substitute.For<IDistributedCacheService>();
        _logger = Substitute.For<ILogger<GetInspectionByIdQueryHandler>>();
        _handler = new GetInspectionByIdQueryHandler(_repository, _cache, _logger);
    }

    [Fact]
    public async Task Handle_CachedInspection_ReturnsCachedData()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var cachedDto = new InspectionDto
        {
            Id = inspectionId,
            Title = "Cached Inspection",
            Description = "From cache",
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "user@example.com",
            Items = new List<InspectionItemDto>()
        };

        var query = new GetInspectionByIdQuery(inspectionId);
        var cacheKey = $"inspection:{inspectionId}";

        _cache.GetAsync<InspectionDto>(cacheKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InspectionDto?>(cachedDto));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedDto);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotCached_FetchesFromRepositoryAndCaches()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Test Inspection",
            "Test Description",
            "creator@example.com");

        inspection.AddItem("Item 1", "Description 1", 1);

        var query = new GetInspectionByIdQuery(inspectionId);
        var cacheKey = $"inspection:{inspectionId}";

        _cache.GetAsync<InspectionDto>(cacheKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InspectionDto?>(null));

        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(inspection)));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Test Inspection");
        result.Value.Description.Should().Be("Test Description");
        result.Value.Items.Should().HaveCount(1);

        await _repository.Received(1).GetByIdAsync(inspectionId, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            cacheKey,
            Arg.Any<InspectionDto>(),
            Arg.Is<TimeSpan>(ts => ts == TimeSpan.FromMinutes(15)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InspectionNotFound_ReturnsFailure()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var query = new GetInspectionByIdQuery(inspectionId);
        var cacheKey = $"inspection:{inspectionId}";

        _cache.GetAsync<InspectionDto>(cacheKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InspectionDto?>(null));

        var errorMessage = $"Inspection with ID {inspectionId} not found";
        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<Inspection>(errorMessage)));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<InspectionDto>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_ReturnsFailure()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var query = new GetInspectionByIdQuery(inspectionId);
        var cacheKey = $"inspection:{inspectionId}";

        _cache.GetAsync<InspectionDto>(cacheKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InspectionDto?>(null));

        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to retrieve inspection");
    }

    [Fact]
    public async Task Handle_InspectionWithMultipleItems_MapsAllItems()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = Inspection.Create(
            "Multi-Item Inspection",
            "Inspection with multiple items",
            "creator@example.com");

        inspection.AddItem("Item 1", "Description 1", 1);
        inspection.AddItem("Item 2", "Description 2", 2);
        inspection.AddItem("Item 3", "Description 3", 3);

        var query = new GetInspectionByIdQuery(inspectionId);
        var cacheKey = $"inspection:{inspectionId}";

        _cache.GetAsync<InspectionDto>(cacheKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InspectionDto?>(null));

        _repository.GetByIdAsync(inspectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(inspection)));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().Contain(i => i.Name == "Item 1" && i.Order == 1);
        result.Value.Items.Should().Contain(i => i.Name == "Item 2" && i.Order == 2);
        result.Value.Items.Should().Contain(i => i.Name == "Item 3" && i.Order == 3);
    }

    [Fact]
    public async Task Handle_CacheThrowsException_ReturnsFailure()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var query = new GetInspectionByIdQuery(inspectionId);
        var cacheKey = $"inspection:{inspectionId}";

        _cache.GetAsync<InspectionDto>(cacheKey, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cache unavailable"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to retrieve inspection");
    }
}
