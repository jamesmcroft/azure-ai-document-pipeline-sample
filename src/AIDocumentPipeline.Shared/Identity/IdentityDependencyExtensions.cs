using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AIDocumentPipeline.Shared.Identity;

public static class IdentityDependencyExtensions
{
    public static IServiceCollection AddAzureCredential(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = AzureIdentitySettings.FromConfiguration(configuration);
        services.TryAddSingleton(_ => settings);

        services.TryAddSingleton(_ =>
        {
            var credentialOpts = new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeSharedTokenCacheCredential = true,
                ExcludeAzureDeveloperCliCredential = true,
                ExcludeAzurePowerShellCredential = true,
                ExcludeWorkloadIdentityCredential = true,
                CredentialProcessTimeout = TimeSpan.FromSeconds(10)
            };

            if (!string.IsNullOrEmpty(settings.ManagedIdentityClientId))
            {
                credentialOpts.ManagedIdentityClientId = settings.ManagedIdentityClientId;
            }

            return new DefaultAzureCredential(credentialOpts);
        });

        return services;
    }
}
