using InspectionService.Api.Middleware;
using InspectionService.Application;
using InspectionService.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting InspectionService.Api");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();

    // Add API documentation (skip in Testing environment)
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }

    // Add authentication with Azure Entra ID (skip in Testing environment)
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureEntraId"));
    }

    // Add authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("CanCreateInspection", policy =>
            policy.RequireAuthenticatedUser());
        
        options.AddPolicy("CanCompleteInspection", policy =>
            policy.RequireAuthenticatedUser());
        
        options.AddPolicy("CanViewAllInspections", policy =>
            policy.RequireAuthenticatedUser());
    });

    // Add health checks (skip in Testing environment)
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddHealthChecks()
            .AddNpgSql(
                builder.Configuration.GetConnectionString("DefaultConnection")!,
                name: "database",
                tags: new[] { "db", "postgresql" })
            .AddRedis(
                builder.Configuration.GetConnectionString("Redis")!,
                name: "redis",
                tags: new[] { "cache", "redis" })
            .AddAzureServiceBusTopic(
                builder.Configuration.GetConnectionString("ServiceBus")!,
                topicName: "inspection-events",
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
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Inspection Service API v1");
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

    // Map health check endpoints (skip in Testing environment)
    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new()
        {
            Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("cache")
        });
        app.MapHealthChecks("/health/live", new()
        {
            Predicate = _ => false
        });
    }

    Log.Information("InspectionService.Api started successfully");
    
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


// Make Program class accessible for testing
public partial class Program { }
