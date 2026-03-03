using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {{ServiceName}}.Domain.{{ModuleName}}.Entities;

namespace {{ServiceName}}.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for {{EntityName}}
/// </summary>
public class {{EntityName}}Configuration : IEntityTypeConfiguration<{{EntityName}}>
{
    public void Configure(EntityTypeBuilder<{{EntityName}}> builder)
    {
        builder.ToTable("{{EntityNamePlural}}");

        builder.HasKey(e => e.Id);

        // Configure properties
        // Example:
        // builder.Property(e => e.Name)
        //     .IsRequired()
        //     .HasMaxLength(200);

        // builder.Property(e => e.Description)
        //     .HasMaxLength(2000);

        // Configure value objects
        // Example:
        // builder.OwnsOne(e => e.Status, status =>
        // {
        //     status.Property(s => s.Value)
        //         .HasColumnName("Status")
        //         .IsRequired()
        //         .HasMaxLength(50);
        // });

        // Configure relationships
        // Example:
        // builder.HasMany(e => e.Items)
        //     .WithOne()
        //     .HasForeignKey(item => item.{{EntityName}}Id)
        //     .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        // Example:
        // builder.HasIndex(e => e.Name);
    }
}
