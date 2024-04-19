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
    /// Adds a Document Intelligence Markdown converter to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration to retrieve settings from.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDocumentIntelligenceMarkdownConverter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAzureCredential(configuration);

        var settings = DocumentIntelligenceSettings.FromConfiguration(configuration);
        services.TryAddSingleton(_ => settings);

        services.TryAddSingleton(sp =>
        {
            var endpoint = sp.GetRequiredService<IConfiguration>()
                               .GetValue<string>(DocumentIntelligenceSettings.EndpointConfigKey) ??
                           throw new InvalidOperationException(
                               $"{DocumentIntelligenceSettings.EndpointConfigKey} is not configured.");

            var credentials = sp.GetRequiredService<DefaultAzureCredential>();
            return new DocumentIntelligenceClient(new Uri(endpoint), credentials);
        });

        services.TryAddSingleton<IDocumentMarkdownConverter, DocumentIntelligenceMarkdownConverter>();

        return services;
    }

    /// <summary>
    /// Adds an Azure OpenAI document data extractor to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="options">The configuration options for the Azure OpenAI document data extractor.</param>
    /// <param name="configuration">The application configuration to retrieve settings from.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddOpenAIDocumentDataExtractor(
        this IServiceCollection services,
        Action<OpenAIDocumentDataExtractionOptions> options,
        IConfiguration configuration)
    {
        services.AddAzureCredential(configuration);

        var settings = OpenAISettings.FromConfiguration(configuration);
        services.TryAddSingleton(_ => settings);

        services.TryAddSingleton(sp =>
        {
            var credentials = sp.GetRequiredService<DefaultAzureCredential>();
            return new OpenAIClient(new Uri(settings.Endpoint), credentials);
        });

        services.Configure(options);

        services.TryAddSingleton<IDocumentDataExtractor, OpenAIDocumentDataExtractor>();

        return services;
    }
}
