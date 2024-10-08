using OpenAI.Chat;

namespace AIDocumentPipeline.Shared.Documents.OpenAI;

/// <summary>
/// Defines the configuration options for the Azure OpenAI document data extraction service.
/// </summary>
public class OpenAIDocumentDataExtractionOptions
    : ChatCompletionOptions
{
    /// <summary>
    /// Initializes the configuration options for the Azure OpenAI document data extraction service.
    /// </summary>
    /// <remarks>
    /// The default values are as follows:
    /// - MaxOutputTokenCount: 4096
    /// - Temperature: 0.1
    /// - TopP: 0.1
    /// </remarks>
    public OpenAIDocumentDataExtractionOptions()
    {
        Messages = [];
        MaxOutputTokenCount = 4096;
        Temperature = 0.1f;
        TopP = 0.1f;
    }

    /// <summary>
    /// Gets or sets the deployment name of the model used for data extraction.
    /// </summary>
    public string? DeploymentName { get; set; }

    /// <summary>
    /// Gets or sets the system prompt used to instruct the model to extract data from documents.
    /// </summary>
    public string SystemPrompt { get; set; } =
        "You are an AI assistant that extracts data from documents.";

    /// <summary>
    /// Gets or sets the list of chat messages used to interact with the model.
    /// </summary>
    public List<ChatMessage> Messages { get; }
}
