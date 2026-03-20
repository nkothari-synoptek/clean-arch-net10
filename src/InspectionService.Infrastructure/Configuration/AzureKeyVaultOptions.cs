namespace InspectionService.Infrastructure.Configuration;

/// <summary>
/// Configuration for Azure Key Vault provider integration.
/// </summary>
public sealed class AzureKeyVaultOptions
{
    public const string SectionName = "AzureKeyVault";

    /// <summary>
    /// Enables or disables Key Vault configuration loading.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Key Vault URI, for example: https://my-vault.vault.azure.net/
    /// </summary>
    public string? VaultUri { get; set; }

    /// <summary>
    /// Optional user-assigned managed identity client id.
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// Enables periodic secret reload from Key Vault at runtime.
    /// </summary>
    public bool ReloadEnabled { get; set; }

    /// <summary>
    /// Interval used to poll Key Vault for updated secret values.
    /// </summary>
    public TimeSpan? ReloadInterval { get; set; }
}
