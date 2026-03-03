using Microsoft.EntityFrameworkCore;
using {{ServiceName}}.Domain.{{ModuleName}}.Entities;

namespace {{ServiceName}}.Infrastructure.Persistence;

/// <summary>
/// Database context for {{ServiceName}}
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Add DbSets here
    // Example:
    // public DbSet<{{EntityName}}> {{EntityNamePlural}} => Set<{{EntityName}}>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
