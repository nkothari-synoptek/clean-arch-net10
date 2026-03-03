using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace Common.Shared.Authentication;

/// <summary>
/// Extension methods for configuring service-to-service authentication.
/// </summary>
public static class ServiceAuthenticationExtensions
{
    /// <summary>
    /// Adds service-to-service authentication handler to an HttpClient.
    /// </summary>
    /// <param name="builder">The HttpClient builder.</param>
    /// <param name="scopeConfigKey">Optional configuration key for the scope.</param>
    /// <returns>The HttpClient builder for chaining.</returns>
    public static IHttpClientBuilder AddServiceAuthentication(
        this IHttpClientBuilder builder,
        string? scopeConfigKey = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddHttpMessageHandler(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ServiceAuthenticationHandler>>();
            
            return new ServiceAuthenticationHandler(configuration, logger, scopeConfigKey);
        });

        return builder;
    }

    /// <summary>
    /// Adds JWT Bearer authentication for validating incoming service-to-service calls.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServiceToServiceAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Add Microsoft Identity Web for Azure AD authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

        return services;
    }

    /// <summary>
    /// Adds authorization policies for service-to-service access control.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurePolicies">Action to configure authorization policies.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServiceAuthorizationPolicies(
        this IServiceCollection services,
        Action<Microsoft.AspNetCore.Authorization.AuthorizationOptions> configurePolicies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurePolicies);

        services.AddAuthorization(configurePolicies);

        return services;
    }

    /// <summary>
    /// Adds a service authorization policy that requires a specific client ID.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    /// <param name="policyName">The policy name.</param>
    /// <param name="allowedClientIds">The allowed client IDs.</param>
    public static void AddServicePolicy(
        this Microsoft.AspNetCore.Authorization.AuthorizationOptions options,
        string policyName,
        params string[] allowedClientIds)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(allowedClientIds);

        if (allowedClientIds.Length == 0)
        {
            throw new ArgumentException("At least one client ID must be specified.", nameof(allowedClientIds));
        }

        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("azp", allowedClientIds);
        });
    }

    /// <summary>
    /// Adds a service authorization policy that requires specific scopes.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    /// <param name="policyName">The policy name.</param>
    /// <param name="requiredScopes">The required scopes.</param>
    public static void AddScopePolicy(
        this Microsoft.AspNetCore.Authorization.AuthorizationOptions options,
        string policyName,
        params string[] requiredScopes)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(requiredScopes);

        if (requiredScopes.Length == 0)
        {
            throw new ArgumentException("At least one scope must be specified.", nameof(requiredScopes));
        }

        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("scp", requiredScopes);
        });
    }
}
