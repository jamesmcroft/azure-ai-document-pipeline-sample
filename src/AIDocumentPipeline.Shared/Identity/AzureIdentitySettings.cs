using Microsoft.Extensions.Configuration;

namespace AIDocumentPipeline.Shared.Identity;

/// <summary>
/// Defines the settings for configuring Azure identity.
/// </summary>
/// <param name="managedIdentityClientId">The client ID of the managed identity for authentication with Azure services.</param>
public class AzureIdentitySettings(
    string? managedIdentityClientId)
{
    /// <summary>
    /// The configuration key for the client ID of the managed identity for authentication with Azure services.
    /// </summary>
    public const string ManagedIdentityClientIdConfigKey = "MANAGED_IDENTITY_CLIENT_ID";

    /// <summary>
    /// Gets the client ID of the managed identity for authentication with Azure services.
    /// </summary>
    public string? ManagedIdentityClientId { get; init; } = managedIdentityClientId;

    /// <summary>
    /// Creates a new instance of the <see cref="AzureIdentitySettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration to retrieve settings from.</param>
    /// <returns>A new instance of the <see cref="AzureIdentitySettings"/> class.</returns>
    public static AzureIdentitySettings FromConfiguration(IConfiguration configuration)
    {
        return new AzureIdentitySettings(configuration[ManagedIdentityClientIdConfigKey]);
    }
}
