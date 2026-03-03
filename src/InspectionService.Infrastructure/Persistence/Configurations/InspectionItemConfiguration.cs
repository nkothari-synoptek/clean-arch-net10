using InspectionService.Domain.Inspections.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InspectionService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for InspectionItem entity
/// </summary>
public class InspectionItemConfiguration : IEntityTypeConfiguration<InspectionItem>
{
    public void Configure(EntityTypeBuilder<InspectionItem> builder)
    {
        builder.ToTable("InspectionItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.InspectionId)
            .IsRequired();

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(i => i.IsCompliant)
            .IsRequired();

        builder.Property(i => i.Notes)
            .HasMaxLength(2000);

        builder.Property(i => i.Order)
            .IsRequired();

        // Create index on InspectionId for faster queries
        builder.HasIndex(i => i.InspectionId);

        // Ignore domain events
        builder.Ignore("_domainEvents");
    }
}
