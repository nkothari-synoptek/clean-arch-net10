using Common.Shared.Caching;
using FluentAssertions;
using InspectionService.Domain.Inspections.Entities;
using InspectionService.Infrastructure.Persistence;
using InspectionService.Infrastructure.Persistence.Repositories.Inspections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace InspectionService.Infrastructure.Tests.Persistence.Repositories;

/// <summary>
/// Integration tests for InspectionRepository using Testcontainers
/// Tests database operations and cache-aside pattern with real SQL Server and Redis
/// </summary>
public class InspectionRepositoryIntegrationTests : IAsyncLifetime
{
    private MsSqlContainer _sqlServerContainer = null!;
    private RedisContainer _redisContainer = null!;
    private ApplicationDbContext _dbContext = null!;
    private IDistributedCacheService _cacheService = null!;
    private InspectionRepository _repository = null!;
    private ILogger<InspectionRepository> _logger = null!;

    public async Task InitializeAsync()
    {
        // Create and start SQL Server container
        _sqlServerContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .Build();

        await _sqlServerContainer.StartAsync();

        // Create and start Redis container
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();

        // Create DbContext with SQL Server
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_sqlServerContainer.GetConnectionString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        // Create real Redis cache service
        var redisConnectionString = _redisContainer.GetConnectionString();
        var redis = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(redisConnectionString);
        var redisLogger = Substitute.For<ILogger<RedisCacheService>>();
        _cacheService = new RedisCacheService(redis, redisLogger);

        // Create repository with real dependencies
        _logger = Substitute.For<ILogger<InspectionRepository>>();
        _repository = new InspectionRepository(_dbContext, _cacheService, _logger);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _sqlServerContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistInspectionToDatabase()
    {
        // Arrange
        var inspection = Inspection.Create(
            "Safety Inspection",
            "Monthly safety check",
            "inspector@test.com");

        inspection.AddItem("Fire Extinguisher", "Check expiry date", 1);
        inspection.AddItem("Emergency Exit", "Verify clear path", 2);

        // Act
        var result = await _repository.AddAsync(inspection);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(inspection.Id);

        // Verify in database
        var savedInspection = await _dbContext.Inspections
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == inspection.Id);

        savedInspection.Should().NotBeNull();
        savedInspection!.Title.Should().Be("Safety Inspection");
        savedInspection.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldRetrieveFromDatabase()
    {
        // Arrange
        var inspection = Inspection.Create(
            "Equipment Inspection",
            "Quarterly equipment check",
            "admin@test.com");

        await _repository.AddAsync(inspection);

        // Act
        var result = await _repository.GetByIdAsync(inspection.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Title.Should().Be("Equipment Inspection");
        result.Value.Id.Should().Be(inspection.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateDatabase()
    {
        // Arrange
        var inspection = Inspection.Create(
            "Original Title",
            "Original Description",
            "creator@test.com");

        await _repository.AddAsync(inspection);

        // Modify inspection
        inspection.UpdateDetails("Updated Title", "Updated Description");

        // Act
        var result = await _repository.UpdateAsync(inspection);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify database was updated
        var updated = await _dbContext.Inspections.FindAsync(inspection.Id);
        updated!.Title.Should().Be("Updated Title");
        updated.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFromDatabase()
    {
        // Arrange
        var inspection = Inspection.Create(
            "To Be Deleted",
            "This will be removed",
            "user@test.com");

        await _repository.AddAsync(inspection);

        // Act
        var result = await _repository.DeleteAsync(inspection.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify removed from database
        var deleted = await _dbContext.Inspections.FindAsync(inspection.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistentId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            var inspection = Inspection.Create(
                $"Inspection {i}",
                $"Description {i}",
                "creator@test.com");
            await _repository.AddAsync(inspection);
        }

        // Act
        var result = await _repository.GetPagedAsync(pageNumber: 2, pageSize: 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPagedAsync_WithStatusFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var draftInspection = Inspection.Create("Draft", "Draft inspection", "user@test.com");
        await _repository.AddAsync(draftInspection);

        var inProgressInspection = Inspection.Create("In Progress", "Active inspection", "user@test.com");
        inProgressInspection.AddItem("Item 1", "Description", 1);
        inProgressInspection.Start();
        await _repository.AddAsync(inProgressInspection);

        // Act
        var draftResults = await _repository.GetPagedAsync(1, 10, "Draft");
        var inProgressResults = await _repository.GetPagedAsync(1, 10, "InProgress");

        // Assert
        draftResults.IsSuccess.Should().BeTrue($"Draft query failed: {draftResults.Error}");
        draftResults.Value.Should().HaveCountGreaterOrEqualTo(1);
        draftResults.Value.Should().OnlyContain(i => i.Status.IsDraft);

        inProgressResults.IsSuccess.Should().BeTrue();
        inProgressResults.Value.Should().HaveCountGreaterOrEqualTo(1);
        inProgressResults.Value.Should().OnlyContain(i => i.Status.IsInProgress);
    }

    [Fact]
    public async Task GetTotalCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        for (int i = 1; i <= 7; i++)
        {
            var inspection = Inspection.Create(
                $"Count Test {i}",
                $"Description {i}",
                "creator@test.com");
            await _repository.AddAsync(inspection);
        }

        // Act
        var count = await _repository.GetTotalCountAsync();

        // Assert
        count.Should().BeGreaterOrEqualTo(7);
    }

    [Fact]
    public async Task GetTotalCountAsync_WithStatusFilter_ShouldReturnFilteredCount()
    {
        // Arrange
        var draft1 = Inspection.Create("Draft 1", "Description", "user@test.com");
        var draft2 = Inspection.Create("Draft 2", "Description", "user@test.com");
        await _repository.AddAsync(draft1);
        await _repository.AddAsync(draft2);

        // Act
        var draftCount = await _repository.GetTotalCountAsync("Draft");

        // Assert
        draftCount.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetByIdAsync_MultipleCallsShouldReturnConsistentResults()
    {
        // Arrange
        var inspection = Inspection.Create(
            "Consistency Test",
            "Testing consistent retrieval",
            "tester@test.com");

        await _repository.AddAsync(inspection);

        // Act & Assert - First call
        var result1 = await _repository.GetByIdAsync(inspection.Id);
        result1.IsSuccess.Should().BeTrue();
        result1.Value.Title.Should().Be("Consistency Test");

        // Act & Assert - Second call should return same data
        var result2 = await _repository.GetByIdAsync(inspection.Id);
        result2.IsSuccess.Should().BeTrue();
        result2.Value.Id.Should().Be(result1.Value.Id);
        result2.Value.Title.Should().Be("Consistency Test");

        // Update the inspection
        inspection.UpdateDetails("Updated", "Updated description");
        await _repository.UpdateAsync(inspection);

        // Act & Assert - After update, should return updated data
        var result3 = await _repository.GetByIdAsync(inspection.Id);
        result3.IsSuccess.Should().BeTrue();
        result3.Value.Title.Should().Be("Updated");
        result3.Value.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task GetByIdAsync_WithInspectionItems_ShouldLoadItemsCorrectly()
    {
        // Arrange
        var inspection = Inspection.Create(
            "Inspection with Items",
            "Testing item loading",
            "creator@test.com");

        inspection.AddItem("Item 1", "First item", 1);
        inspection.AddItem("Item 2", "Second item", 2);
        inspection.AddItem("Item 3", "Third item", 3);

        await _repository.AddAsync(inspection);

        // Act
        var result = await _repository.GetByIdAsync(inspection.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().Contain(i => i.Name == "Item 1");
        result.Value.Items.Should().Contain(i => i.Name == "Item 2");
        result.Value.Items.Should().Contain(i => i.Name == "Item 3");
    }
}
