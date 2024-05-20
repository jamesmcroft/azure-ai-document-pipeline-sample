using AIDocumentPipeline.Shared.Documents.DocumentIntelligence;
using AIDocumentPipeline.Shared.Documents.OpenAI;
using AIDocumentPipeline.Shared.Identity;
using Azure.AI.DocumentIntelligence;
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
    /// <summary>
    /// Adds an Azure OpenAI document data extractor to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="options">The configuration options for the Azure OpenAI document data extractor.</param>
    /// <param name="configuration">The application configuration to retrieve settings from.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddOpenAIMarkdownDocumentDataExtractor(
        this IServiceCollection services,
        Action<OpenAIDocumentDataExtractionOptions> options,
        IConfiguration configuration)
    {
        services.AddAzureCredential(configuration);

        var diSettings = DocumentIntelligenceSettings.FromConfiguration(configuration);
        services.TryAddSingleton(_ => diSettings);

        var oaiSettings = OpenAISettings.FromConfiguration(configuration);
        services.TryAddSingleton(_ => oaiSettings);

        services.TryAddSingleton(sp =>
        {
            var credentials = sp.GetRequiredService<DefaultAzureCredential>();
            return new DocumentIntelligenceClient(new Uri(diSettings.Endpoint), credentials);
        });

        services.TryAddSingleton(sp =>
        {
            var credentials = sp.GetRequiredService<DefaultAzureCredential>();
            return new OpenAIClient(new Uri(oaiSettings.Endpoint), credentials);
        });

        services.Configure(options);

        services.TryAddSingleton<IDocumentMarkdownConverter, DocumentIntelligenceMarkdownConverter>();
        services.TryAddSingleton<IDocumentDataExtractor, OpenAIMarkdownDocumentDataExtractor>();

        return services;
    }

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
            return new OpenAIClient(new Uri(oaiSettings.Endpoint), credentials);
        });

        services.Configure(options);

        services.TryAddSingleton<IDocumentDataExtractor, OpenAIVisionDocumentDataExtractor>();

        return services;

    }
}
