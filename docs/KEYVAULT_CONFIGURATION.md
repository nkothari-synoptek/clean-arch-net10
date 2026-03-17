# Azure Key Vault Configuration (InspectionService)

This service now loads runtime secrets from Azure Key Vault and binds them to a strongly typed options model (`ServiceSecretsOptions`).

## 1. Required app config (non-secret)

Set only Key Vault metadata in `appsettings.*.json` (or env vars):

- `AzureKeyVault:Enabled` = `true`
- `AzureKeyVault:VaultUri` = `https://<vault-name>.vault.azure.net/`
- `AzureKeyVault:ManagedIdentityClientId` = optional, only for user-assigned identity

No secret values should be committed to appsettings.

## 2. Key Vault secret names

Use `--` in secret names to represent `:` in .NET config keys.

- `ServiceSecrets--Database--ConnectionString`
- `ServiceSecrets--Cache--RedisConnectionString`
- `ServiceSecrets--Messaging--ServiceBusConnectionString` (optional)
- `ServiceSecrets--Messaging--ServiceBusFullyQualifiedNamespace` (optional, for MI auth)

## 3. Local development

1. Run `az login`
2. Ensure your user has `Key Vault Secrets User` (or equivalent read access)
3. Set:
   - `ASPNETCORE_ENVIRONMENT=Development`
   - `AzureKeyVault__Enabled=true`
   - `AzureKeyVault__VaultUri=https://<vault-name>.vault.azure.net/`
4. Run the API normally (`dotnet run`)

## 4. EF Core migrations (design time)

`ApplicationDbContextFactory` now resolves the DB connection from `ServiceSecrets:Database:ConnectionString` using the same Key Vault flow.

Example:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:AzureKeyVault__Enabled="true"
$env:AzureKeyVault__VaultUri="https://<vault-name>.vault.azure.net/"
dotnet ef database update --project src/InspectionService.Infrastructure --startup-project src/InspectionService.Api
```

## 5. CI/CD and AKS guidance

- CI/CD: use workload identity/service principal with `get/list` access on required secrets, and set `AzureKeyVault__Enabled=true`, `AzureKeyVault__VaultUri`.
- AKS: use managed identity (prefer workload identity). App pods authenticate to Key Vault at runtime, so secrets are never baked into the Docker image.
- Do not copy secret values into image layers, Helm values, or ConfigMaps.
