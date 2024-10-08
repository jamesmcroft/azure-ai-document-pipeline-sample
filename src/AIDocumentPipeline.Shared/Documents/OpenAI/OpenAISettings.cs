using Microsoft.Extensions.Configuration;

namespace AIDocumentPipeline.Shared.Documents.OpenAI;

/// <summary>
/// Defines the settings for configuring Azure OpenAI.
/// </summary>
/// <param name="endpoint">The endpoint URL for the Azure OpenAI service.</param>
public class OpenAISettings(string endpoint)
{
    /// <summary>
    /// The configuration key for the Azure OpenAI endpoint URL.
    /// </summary>
    public const string EndpointConfigKey = "OPENAI_ENDPOINT";

    /// <summary>
    /// The configuration key for the deployment of a vision completion model.
    /// </summary>
    public const string VisionCompletionModelDeploymentConfigKey = "OPENAI_VISION_COMPLETION_DEPLOYMENT";

    /// <summary>
    /// Gets the endpoint URL for the Azure OpenAI service.
    /// </summary>
    public string Endpoint { get; init; } = endpoint;

    /// <summary>
    /// Creates a new instance of the <see cref="OpenAISettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <returns>A new instance of the <see cref="OpenAISettings"/> class.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the required configuration is not present.</exception>
    public static OpenAISettings FromConfiguration(IConfiguration configuration)
    {
        var configEndpoint = configuration[EndpointConfigKey] ??
                             throw new InvalidOperationException($"{EndpointConfigKey} is not configured.");

        return new OpenAISettings(configEndpoint);
    }
}
