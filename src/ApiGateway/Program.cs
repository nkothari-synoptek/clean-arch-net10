using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ApiGateway")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add Azure Entra External JWT Bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["AzureAd:Authority"];
        options.Audience = builder.Configuration["AzureAd:ClientId"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["AzureAd:Authority"],
            ValidAudience = builder.Configuration["AzureAd:ClientId"]
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("Authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Information("Token validated for user: {User}", 
                    context.Principal?.Identity?.Name ?? "Unknown");
                return Task.CompletedTask;
            }
        };
    });

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    // Policy for creating inspections - requires Inspector or Admin role
    options.AddPolicy("CanCreateInspection", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Inspector", "Admin")); // Use RequireRole instead of RequireClaim
    
    // Policy for completing inspections - requires Inspector role
    options.AddPolicy("CanCompleteInspection", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Inspector")); // Use RequireRole instead of RequireClaim
    
    // Policy for viewing all inspections - requires Admin or Supervisor role
    options.AddPolicy("CanViewAllInspections", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin", "Supervisor")); // Use RequireRole instead of RequireClaim
});

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Use user identity if authenticated, otherwise use IP address
        var partitionKey = context.User.Identity?.Name 
            ?? context.Connection.RemoteIpAddress?.ToString() 
            ?? "anonymous";
        
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            });
    });
    
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        
        Log.Warning("Rate limit exceeded for {User}", 
            context.HttpContext.User.Identity?.Name ?? "Anonymous");
        
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many requests. Please try again later." },
            cancellationToken: cancellationToken);
    };
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Add Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserName", httpContext.User.Identity.Name);
        }
    };
});

// Apply rate limiting
app.UseRateLimiter();

// Apply authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoint
app.MapHealthChecks("/health");

// Map reverse proxy
app.MapReverseProxy();

try
{
    Log.Information("Starting API Gateway");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for testing
public partial class Program { }
