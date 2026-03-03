using FluentAssertions;
using InspectionService.Domain.Inspections.Entities;
using InspectionService.Domain.Inspections.ValueObjects;

namespace InspectionService.Domain.Tests.Inspections;

public class InspectionTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateInspection()
    {
        // Arrange
        var title = "Safety Inspection";
        var description = "Monthly safety inspection";
        var createdBy = "inspector@example.com";

        // Act
        var inspection = Inspection.Create(title, description, createdBy);

        // Assert
        inspection.Should().NotBeNull();
        inspection.Id.Should().NotBeEmpty();
        inspection.Title.Should().Be(title);
        inspection.Description.Should().Be(description);
        inspection.CreatedBy.Should().Be(createdBy);
        inspection.Status.Should().Be(InspectionStatus.Draft);
        inspection.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        inspection.CompletedAt.Should().BeNull();
        inspection.CompletedBy.Should().BeNull();
        inspection.Items.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithValidParameters_ShouldRaiseInspectionCreatedEvent()
    {
        // Arrange
        var title = "Safety Inspection";
        var description = "Monthly safety inspection";
        var createdBy = "inspector@example.com";

        // Act
        var inspection = Inspection.Create(title, description, createdBy);

        // Assert
        inspection.DomainEvents.Should().HaveCount(1);
        var domainEvent = inspection.DomainEvents.First();
        domainEvent.Should().BeOfType<InspectionService.Domain.Inspections.Events.InspectionCreatedEvent>();
    }

    [Theory]
    [InlineData(null, "description", "user")]
    [InlineData("", "description", "user")]
    [InlineData("   ", "description", "user")]
    public void Create_WithInvalidTitle_ShouldThrowArgumentException(string title, string description, string createdBy)
    {
        // Act
        var act = () => Inspection.Create(title, description, createdBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*title*");
    }

    [Theory]
    [InlineData("title", null, "user")]
    [InlineData("title", "", "user")]
    [InlineData("title", "   ", "user")]
    public void Create_WithInvalidDescription_ShouldThrowArgumentException(string title, string description, string createdBy)
    {
        // Act
        var act = () => Inspection.Create(title, description, createdBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*description*");
    }

    [Theory]
    [InlineData("title", "description", null)]
    [InlineData("title", "description", "")]
    [InlineData("title", "description", "   ")]
    public void Create_WithInvalidCreatedBy_ShouldThrowArgumentException(string title, string description, string createdBy)
    {
        // Act
        var act = () => Inspection.Create(title, description, createdBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*createdBy*");
    }

    [Fact]
    public void Complete_WithValidState_ShouldCompleteInspection()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        inspection.Start();
        
        // Mark all items as reviewed
        var item = inspection.Items.First();
        item.MarkAsCompliant("All good");
        
        var completedBy = "completer@example.com";

        // Act
        var result = inspection.Complete(completedBy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inspection.Status.Should().Be(InspectionStatus.Completed);
        inspection.CompletedBy.Should().Be(completedBy);
        inspection.CompletedAt.Should().NotBeNull();
        inspection.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Complete_WithValidState_ShouldRaiseInspectionCompletedEvent()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        inspection.Start();
        
        var item = inspection.Items.First();
        item.MarkAsCompliant("All good");
        
        inspection.ClearDomainEvents(); // Clear creation event

        // Act
        var result = inspection.Complete("completer@example.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        inspection.DomainEvents.Should().HaveCount(1);
        var domainEvent = inspection.DomainEvents.First();
        domainEvent.Should().BeOfType<InspectionService.Domain.Inspections.Events.InspectionCompletedEvent>();
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldReturnFailure()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        inspection.Start();
        
        var item = inspection.Items.First();
        item.MarkAsCompliant("All good");
        
        inspection.Complete("completer@example.com");

        // Act
        var result = inspection.Complete("another@example.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Inspection is already completed.");
    }

    [Fact]
    public void Complete_WhenCancelled_ShouldReturnFailure()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        inspection.Cancel();

        // Act
        var result = inspection.Complete("completer@example.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Cannot complete a cancelled inspection.");
    }

    [Fact]
    public void Complete_WithoutItems_ShouldReturnFailure()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");

        // Act
        var result = inspection.Complete("completer@example.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Cannot complete an inspection without items.");
    }

    [Fact]
    public void Complete_WithUnreviewedItems_ShouldReturnFailure()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        inspection.AddItem("Item 2", "Description 2", 2);
        inspection.Start();
        
        // Only mark one item as reviewed
        var firstItem = inspection.Items.First();
        firstItem.MarkAsCompliant("All good");

        // Act
        var result = inspection.Complete("completer@example.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("All items must be reviewed before completing the inspection.");
    }

    [Fact]
    public void Complete_WithAllItemsReviewedAsCompliant_ShouldSucceed()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        inspection.AddItem("Item 2", "Description 2", 2);
        inspection.Start();
        
        foreach (var item in inspection.Items)
        {
            item.MarkAsCompliant("Reviewed");
        }

        // Act
        var result = inspection.Complete("completer@example.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Complete_WithAllItemsReviewedWithNotes_ShouldSucceed()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        inspection.AddItem("Item 2", "Description 2", 2);
        inspection.Start();
        
        var items = inspection.Items.ToList();
        items[0].MarkAsCompliant("Good");
        items[1].MarkAsNonCompliant("Needs improvement");

        // Act
        var result = inspection.Complete("completer@example.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Complete_WithInvalidCompletedBy_ShouldThrowArgumentException(string completedBy)
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        inspection.Start();
        
        var item = inspection.Items.First();
        item.MarkAsCompliant("All good");

        // Act
        var act = () => inspection.Complete(completedBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*completedBy*");
    }

    [Fact]
    public void Start_FromDraftWithItems_ShouldSucceed()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);

        // Act
        var result = inspection.Start();

        // Assert
        result.IsSuccess.Should().BeTrue();
        inspection.Status.Should().Be(InspectionStatus.InProgress);
    }

    [Fact]
    public void Start_FromDraftWithoutItems_ShouldReturnFailure()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");

        // Act
        var result = inspection.Start();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Cannot start an inspection without items.");
    }

    [Fact]
    public void Start_WhenNotDraft_ShouldReturnFailure()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        inspection.Start();

        // Act
        var result = inspection.Start();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Only draft inspections can be started.");
    }

    [Fact]
    public void AddItem_ToDraftInspection_ShouldSucceed()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");

        // Act
        var result = inspection.AddItem("Item 1", "Description 1", 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inspection.Items.Should().HaveCount(1);
        inspection.Items.First().Name.Should().Be("Item 1");
    }

    [Fact]
    public void AddItem_ToCompletedInspection_ShouldReturnFailure()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        inspection.Start();
        
        var item = inspection.Items.First();
        item.MarkAsCompliant("All good");
        
        inspection.Complete("completer@example.com");

        // Act
        var result = inspection.AddItem("Item 2", "Description 2", 2);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Cannot add items to a completed or cancelled inspection.");
    }

    [Fact]
    public void AddItem_ToCancelledInspection_ShouldReturnFailure()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.Cancel();

        // Act
        var result = inspection.AddItem("Item 1", "Description 1", 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Cannot add items to a completed or cancelled inspection.");
    }

    [Fact]
    public void RemoveItem_FromDraftInspection_ShouldSucceed()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        var itemId = inspection.Items.First().Id;

        // Act
        var result = inspection.RemoveItem(itemId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        inspection.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_FromCompletedInspection_ShouldReturnFailure()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        var itemId = inspection.Items.First().Id;
        inspection.Start();
        
        var item = inspection.Items.First();
        item.MarkAsCompliant("All good");
        
        inspection.Complete("completer@example.com");

        // Act
        var result = inspection.RemoveItem(itemId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Cannot remove items from a completed or cancelled inspection.");
    }

    [Fact]
    public void RemoveItem_WithNonExistentId_ShouldReturnFailure()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = inspection.RemoveItem(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void Cancel_FromDraft_ShouldSucceed()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");

        // Act
        var result = inspection.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        inspection.Status.Should().Be(InspectionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenCompleted_ShouldReturnFailure()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.AddItem("Item 1", "Description 1", 1);
        inspection.Start();
        
        var item = inspection.Items.First();
        item.MarkAsCompliant("All good");
        
        inspection.Complete("completer@example.com");

        // Act
        var result = inspection.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Cannot cancel a completed inspection.");
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldReturnFailure()
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");
        inspection.Cancel();

        // Act
        var result = inspection.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Inspection is already cancelled.");
    }

    [Fact]
    public void UpdateDetails_WithValidParameters_ShouldUpdateTitleAndDescription()
    {
        // Arrange
        var inspection = Inspection.Create("Old Title", "Old Description", "creator@example.com");
        var newTitle = "New Title";
        var newDescription = "New Description";

        // Act
        inspection.UpdateDetails(newTitle, newDescription);

        // Assert
        inspection.Title.Should().Be(newTitle);
        inspection.Description.Should().Be(newDescription);
    }

    [Theory]
    [InlineData(null, "description")]
    [InlineData("", "description")]
    [InlineData("   ", "description")]
    public void UpdateDetails_WithInvalidTitle_ShouldThrowArgumentException(string title, string description)
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");

        // Act
        var act = () => inspection.UpdateDetails(title, description);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*title*");
    }

    [Theory]
    [InlineData("title", null)]
    [InlineData("title", "")]
    [InlineData("title", "   ")]
    public void UpdateDetails_WithInvalidDescription_ShouldThrowArgumentException(string title, string description)
    {
        // Arrange
        var inspection = Inspection.Create("Title", "Description", "creator@example.com");

        // Act
        var act = () => inspection.UpdateDetails(title, description);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*description*");
    }
}
