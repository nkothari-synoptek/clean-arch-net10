using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InspectionService.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating ApplicationDbContext instances for EF Core tools
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use PostgreSQL with a placeholder connection string for migrations
        // This will be replaced with actual connection string at runtime
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=InspectionServiceDb;Username=postgres;Password=postgres",
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

