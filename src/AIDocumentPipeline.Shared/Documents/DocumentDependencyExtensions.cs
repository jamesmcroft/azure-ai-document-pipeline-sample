using AIDocumentPipeline.Shared.Documents.OpenAI;
using AIDocumentPipeline.Shared.Identity;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AIDocumentPipeline.Shared.Documents;

/// <summary>
/// Defines a set of extension methods for configuring document dependencies.
/// </summary>
public static class DocumentDependencyExtensions
{
    public static IServiceCollection AddOpenAIVisionDocumentDataExtractor(
        this IServiceCollection services,
        Action<OpenAIDocumentDataExtractionOptions> options,
        IConfiguration configuration)
    {
        services.AddAzureCredential(configuration);

        var oaiSettings = OpenAISettings.FromConfiguration(configuration);
        services.TryAddSingleton(_ => oaiSettings);

        services.TryAddSingleton(sp =>
        {
            var credentials = sp.GetRequiredService<DefaultAzureCredential>();
            return new AzureOpenAIClient(new Uri(oaiSettings.Endpoint), credentials);
        });

        services.Configure(options);

        services.TryAddSingleton<IDocumentDataExtractor, OpenAIVisionDocumentDataExtractor>();

        return services;

    }
}
