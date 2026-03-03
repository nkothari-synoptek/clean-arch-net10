using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Common.Shared.Authentication;

/// <summary>
/// HTTP message handler that adds OAuth 2.0 bearer tokens for service-to-service authentication.
/// Uses Azure.Identity for token acquisition with managed identity support.
/// </summary>
public sealed class ServiceAuthenticationHandler : DelegatingHandler
{
    private readonly TokenCredential _credential;
    private readonly string[] _scopes;
    private readonly ILogger<ServiceAuthenticationHandler> _logger;

    public ServiceAuthenticationHandler(
        IConfiguration configuration,
        ILogger<ServiceAuthenticationHandler> logger,
        string? scopeConfigKey = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        // Use DefaultAzureCredential for automatic credential chain
        // (Managed Identity -> Azure CLI -> Visual Studio -> etc.)
        _credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeVisualStudioCredential = false,
            ExcludeVisualStudioCodeCredential = false,
            ExcludeAzureCliCredential = false,
            ExcludeAzurePowerShellCredential = true,
            ExcludeInteractiveBrowserCredential = true
        });

        // Get scopes from configuration
        var scopeKey = scopeConfigKey ?? "AzureAd:ServiceScope";
        var scope = configuration[scopeKey]
            ?? throw new InvalidOperationException($"Service scope not configured at '{scopeKey}'");

        _scopes = new[] { scope };
    }

    /// <summary>
    /// Constructor with explicit credential and scopes for testing.
    /// </summary>
    public ServiceAuthenticationHandler(
        TokenCredential credential,
        string[] scopes,
        ILogger<ServiceAuthenticationHandler> logger)
    {
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Acquire token
            var tokenContext = new TokenRequestContext(_scopes);
            var token = await _credential.GetTokenAsync(tokenContext, cancellationToken);

            // Add bearer token to request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

            _logger.LogDebug(
                "Added service authentication token for request to {RequestUri}",
                request.RequestUri);

            return await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to acquire service authentication token for request to {RequestUri}",
                request.RequestUri);
            throw;
        }
    }
}
