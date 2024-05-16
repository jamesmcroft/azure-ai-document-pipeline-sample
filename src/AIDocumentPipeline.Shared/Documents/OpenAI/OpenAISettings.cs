using Microsoft.Extensions.Configuration;

namespace AIDocumentPipeline.Shared.Documents.OpenAI;

/// <summary>
/// Defines the settings for configuring Azure OpenAI.
/// </summary>
/// <param name="endpoint">The endpoint URL for the Azure OpenAI service.</param>
/// <param name="embeddingDeploymentName">The deployment of an embedding model.</param>
/// <param name="completionDeploymentName">The deployment of a completion model.</param>
/// <param name="visionCompletionDeploymentName">The deployment of a vision completion model.</param>
public class OpenAISettings(
    string endpoint,
    string? embeddingDeploymentName = null,
    string? completionDeploymentName = null,
    string? visionCompletionDeploymentName = null)
{
    /// <summary>
    /// The configuration key for the Azure OpenAI endpoint URL.
    /// </summary>
    public const string EndpointConfigKey = "OPENAI_ENDPOINT";

    /// <summary>
    /// The configuration key for the deployment of an embedding model.
    /// </summary>
    public const string EmbeddingModelDeploymentConfigKey = "OPENAI_EMBEDDING_DEPLOYMENT";

    /// <summary>
    /// The configuration key for the deployment of a completion model.
    /// </summary>
    public const string CompletionModelDeploymentConfigKey = "OPENAI_COMPLETION_DEPLOYMENT";

    /// <summary>
    /// The configuration key for the deployment of a vision completion model.
    /// </summary>
    public const string VisionCompletionModelDeploymentConfigKey = "OPENAI_VISION_COMPLETION_DEPLOYMENT";

    /// <summary>
    /// Gets the endpoint URL for the Azure OpenAI service.
    /// </summary>
    public string Endpoint { get; init; } = endpoint;

    /// <summary>
    /// Gets the name of the deployment for an embedding model, e.g., text-embedding-ada-002.
    /// </summary>
    public string? EmbeddingDeploymentName { get; init; } = embeddingDeploymentName;

    /// <summary>
    /// Gets the name of the deployment for a completion model, e.g., gpt-35-turbo.
    /// </summary>
    public string? CompletionDeploymentName { get; init; } = completionDeploymentName;

    /// <summary>
    /// Gets the name of the deployment for a vision completion model, e.g., gpt-4.
    /// </summary>
    public string? VisionCompletionDeploymentName { get; init; } = visionCompletionDeploymentName;

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

        return new OpenAISettings(
            configEndpoint,
            configuration[EmbeddingModelDeploymentConfigKey],
            configuration[CompletionModelDeploymentConfigKey],
            configuration[VisionCompletionModelDeploymentConfigKey]);
    }
}
