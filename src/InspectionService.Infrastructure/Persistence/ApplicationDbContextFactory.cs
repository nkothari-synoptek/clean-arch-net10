using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using InspectionService.Infrastructure.Configuration;

namespace InspectionService.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tools for migrations and database updates.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // Resolve startup project config path when running from solution root or Infrastructure project folder.
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiProjectPath = Path.GetFullPath(Path.Combine(currentDirectory, "..", "InspectionService.Api"));
        if (!Directory.Exists(apiProjectPath))
        {
            apiProjectPath = Path.GetFullPath(Path.Combine(currentDirectory, "src", "InspectionService.Api"));
        }

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables();

        var preKeyVaultConfiguration = configurationBuilder.Build();
        configurationBuilder.AddAzureKeyVaultIfConfigured(preKeyVaultConfiguration);

        var configuration = configurationBuilder.Build();

        var serviceSecrets = configuration
            .GetRequiredSection(ServiceSecretsOptions.SectionName)
            .Get<ServiceSecretsOptions>()
            ?? throw new InvalidOperationException(
                $"{ServiceSecretsOptions.SectionName} configuration section was not found.");

        var connectionString = serviceSecrets.Database.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{ServiceSecretsOptions.SectionName}:Database:ConnectionString was not found.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
