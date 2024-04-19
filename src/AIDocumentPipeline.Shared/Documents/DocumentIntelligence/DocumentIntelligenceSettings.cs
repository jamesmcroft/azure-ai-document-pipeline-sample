using Microsoft.Extensions.Configuration;

namespace AIDocumentPipeline.Shared.Documents.DocumentIntelligence;

/// <summary>
/// Defines the configuration settings for the Azure AI Document Intelligence service.
/// </summary>
/// <param name="endpoint">The URL of the Azure AI Document Intelligence endpoint.</param>
public class DocumentIntelligenceSettings(
    string endpoint)
{
    /// <summary>
    /// The configuration key for the Azure AI Document Intelligence endpoint.
    /// </summary>
    public const string EndpointConfigKey = "DOCUMENT_INTELLIGENCE_ENDPOINT";

    /// <summary>
    /// Gets the URL of the Azure AI Document Intelligence endpoint.
    /// </summary>
    public string Endpoint { get; init; } = endpoint;

    public static DocumentIntelligenceSettings FromConfiguration(IConfiguration configuration)
    {
        var configEndpoint = configuration.GetValue<string>(EndpointConfigKey) ??
                             throw new InvalidOperationException(
                                 $"{EndpointConfigKey} is not configured.");

        return new DocumentIntelligenceSettings(configEndpoint);
    }
}
