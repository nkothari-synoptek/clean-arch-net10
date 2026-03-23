using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.Extensions.Configuration;

namespace InspectionService.Infrastructure.Configuration;

public static class KeyVaultConfigurationExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration provider when enabled.
    /// </summary>
    public static ConfigurationManager AddAzureKeyVaultIfConfigured(this ConfigurationManager configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var keyVaultOptions = configuration.GetSection(AzureKeyVaultOptions.SectionName).Get<AzureKeyVaultOptions>();
        if (keyVaultOptions?.Enabled != true || string.IsNullOrWhiteSpace(keyVaultOptions.VaultUri))
        {
            return configuration;
        }

        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(keyVaultOptions.ManagedIdentityClientId))
        {
            credentialOptions.ManagedIdentityClientId = keyVaultOptions.ManagedIdentityClientId;
        }

        var keyVaultConfigurationOptions = CreateConfigurationOptions(keyVaultOptions);
        configuration.AddAzureKeyVault(
            new Uri(keyVaultOptions.VaultUri),
            new DefaultAzureCredential(credentialOptions),
            keyVaultConfigurationOptions);

        return configuration;
    }

    /// <summary>
    /// Adds Azure Key Vault as a configuration provider when enabled.
    /// </summary>
    public static IConfigurationBuilder AddAzureKeyVaultIfConfigured(
        this IConfigurationBuilder configurationBuilder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ArgumentNullException.ThrowIfNull(configuration);

        var keyVaultOptions = configuration.GetSection(AzureKeyVaultOptions.SectionName).Get<AzureKeyVaultOptions>();
        if (keyVaultOptions?.Enabled != true || string.IsNullOrWhiteSpace(keyVaultOptions.VaultUri))
        {
            return configurationBuilder;
        }

        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(keyVaultOptions.ManagedIdentityClientId))
        {
            credentialOptions.ManagedIdentityClientId = keyVaultOptions.ManagedIdentityClientId;
        }

        var keyVaultConfigurationOptions = CreateConfigurationOptions(keyVaultOptions);
        configurationBuilder.AddAzureKeyVault(
            new Uri(keyVaultOptions.VaultUri),
            new DefaultAzureCredential(credentialOptions),
            keyVaultConfigurationOptions);

        return configurationBuilder;
    }

    private static AzureKeyVaultConfigurationOptions CreateConfigurationOptions(AzureKeyVaultOptions keyVaultOptions)
    {
        return new AzureKeyVaultConfigurationOptions
        {
            ReloadInterval = keyVaultOptions.ReloadEnabled
                ? keyVaultOptions.ReloadInterval ?? TimeSpan.FromMinutes(5)
                : null
        };
    }
}
