using Common.Shared.Configuration;
using {{ServiceName}}.Api.Middleware;
using {{ServiceName}}.Application;
using {{ServiceName}}.Infrastructure;
using {{ServiceName}}.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Serilog;

// Bootstrap console logger so Key Vault failures are visible before full Serilog is configured
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

try
{
    builder.Configuration.AddAzureKeyVaultIfConfigured();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to load Azure Key Vault configuration. Check AzureKeyVault settings and credentials.");
    Log.CloseAndFlush();
    return;
}

// Reconfigure Serilog with full settings now that Key Vault secrets are available
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

try
{
    Log.Information("Starting {{ServiceName}}.Api");

    // Configure Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();

    // Add API documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "{{ServiceName}} API",
            Version = "v1",
            Description = "API for managing {{ServiceName}} operations"
        });
    });

    // Add authentication with Azure Entra ID
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureEntraId"));

    // Add authorization policies
    builder.Services.AddAuthorization(options =>
    {
        // Add your authorization policies here
        // Example:
        // options.AddPolicy("CanCreate{{EntityName}}", policy =>
        //     policy.RequireAuthenticatedUser());
        
        // options.AddPolicy("CanUpdate{{EntityName}}", policy =>
        //     policy.RequireAuthenticatedUser());
        
        // options.AddPolicy("CanDelete{{EntityName}}", policy =>
        //     policy.RequireAuthenticatedUser());
    });

    // Add health checks conditionally based on configured secrets.
    // TODO: Replace with custom health check classes that use IOptionsMonitor<ServiceSecretsOptions>
    // for secret-aware health checks (see InspectionService.Api/HealthChecks for examples).
    var dbConnectionString = builder.Configuration.GetDatabaseConnectionString();
    var redisConnectionString = builder.Configuration.GetRedisConnectionString();
    var serviceBusConnectionString = builder.Configuration.GetServiceBusConnectionString();

    var healthCheckBuilder = builder.Services.AddHealthChecks();

    if (!string.IsNullOrWhiteSpace(dbConnectionString))
    {
        healthCheckBuilder.AddNpgSql(
            dbConnectionString,
            name: "database",
            tags: new[] { "db", "postgresql" });
    }

    if (!string.IsNullOrWhiteSpace(redisConnectionString))
    {
        healthCheckBuilder.AddRedis(
            redisConnectionString,
            name: "redis",
            tags: new[] { "cache", "redis" });
    }

    if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
    {
        healthCheckBuilder.AddAzureServiceBusTopic(
            serviceBusConnectionString,
            topicName: "{{serviceName}}-events",
            name: "servicebus",
            tags: new[] { "messaging", "servicebus" });
    }

    // Register Application layer services
    builder.Services.AddApplication();

    // Register Infrastructure layer services
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "{{ServiceName}} API v1");
        });
    }

    // Add exception handling middleware
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        };
    });

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Map health check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new()
    {
        Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("cache")
    });
    app.MapHealthChecks("/health/live", new()
    {
        Predicate = _ => false
    });

    Log.Information("{{ServiceName}}.Api started successfully");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
