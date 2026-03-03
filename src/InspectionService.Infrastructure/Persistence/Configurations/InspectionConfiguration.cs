using InspectionService.Domain.Inspections.Entities;
using InspectionService.Domain.Inspections.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InspectionService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Inspection aggregate root
/// </summary>
public class InspectionConfiguration : IEntityTypeConfiguration<Inspection>
{
    public void Configure(EntityTypeBuilder<Inspection> builder)
    {
        builder.ToTable("Inspections");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Description)
            .IsRequired()
            .HasMaxLength(2000);

        // Configure InspectionStatus as a value object
        builder.Property(i => i.Status)
            .HasConversion(
                status => status.Value,
                value => value == "Draft" ? InspectionStatus.Draft :
                         value == "InProgress" ? InspectionStatus.InProgress :
                         value == "Completed" ? InspectionStatus.Completed :
                         InspectionStatus.Cancelled)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.CompletedAt);

        builder.Property(i => i.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.CompletedBy)
            .HasMaxLength(100);

        // Configure the relationship with InspectionItems
        // EF Core will automatically use the backing field _items when accessing Items property
        builder.HasMany(i => i.Items)
            .WithOne()
            .HasForeignKey(item => item.InspectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events (they are not persisted)
        builder.Ignore("_domainEvents");
    }
}
