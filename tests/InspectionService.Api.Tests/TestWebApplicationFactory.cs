using InspectionService.Application.Inspections.Interfaces;
using InspectionService.Domain.Inspections.Entities;
using InspectionService.Shared.Kernel.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InspectionService.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public IInspectionRepository MockRepository { get; } = Substitute.For<IInspectionRepository>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing FIRST so Program.cs can skip problematic services
        builder.UseEnvironment("Testing");
        
        // Configure app settings to disable features that cause issues in tests
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["ConnectionStrings:ServiceBus"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test",
                // Add minimal Azure Entra ID config to prevent startup errors
                ["AzureEntraId:Instance"] = "https://login.microsoftonline.com/",
                ["AzureEntraId:Domain"] = "test.onmicrosoft.com",
                ["AzureEntraId:TenantId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureEntraId:ClientId"] = "00000000-0000-0000-0000-000000000000"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the real repository registration
            services.RemoveAll<IInspectionRepository>();
            
            // Add mock repository
            services.AddSingleton(MockRepository);

            // Remove Azure Entra ID authentication and replace with test authentication
            services.RemoveAll<Microsoft.Identity.Web.MicrosoftIdentityOptions>();
            
            // Remove database context to prevent connection attempts
            services.RemoveAll(typeof(DbContext));
            
            // Remove caching services
            services.RemoveAll<Common.Shared.Caching.IDistributedCacheService>();
            
            // Add test authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });
        
        // Suppress host startup exceptions for testing
        builder.UseSetting("SuppressStatusMessages", "True");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Suppress logging during tests
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
        
        return base.CreateHost(builder);
    }
}
