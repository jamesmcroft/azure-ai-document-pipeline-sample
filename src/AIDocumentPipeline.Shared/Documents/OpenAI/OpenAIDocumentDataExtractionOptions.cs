using Azure.AI.OpenAI;

namespace AIDocumentPipeline.Shared.Documents.OpenAI;

/// <summary>
/// Defines the configuration options for the Azure OpenAI document data extraction service.
/// </summary>
public class OpenAIDocumentDataExtractionOptions
    : ChatCompletionsOptions
{
    /// <summary>
    /// Initializes the configuration options for the Azure OpenAI document data extraction service.
    /// </summary>
    /// <remarks>
    /// The default values are as follows:
    /// - MaxTokens: 4096
    /// - Temperature: 0.1
    /// - NucleusSamplingFactor: 0.1
    /// </remarks>
    public OpenAIDocumentDataExtractionOptions()
    {
        MaxTokens = 4096;
        Temperature = 0.1f;
        NucleusSamplingFactor = 0.1f;
    }

    /// <summary>
    /// Gets or sets the system prompt used to instruct the model to extract data from documents.
    /// </summary>
    public string SystemPrompt { get; set; } =
        "You are an AI assistant that extracts data from documents and returns them as structured JSON objects. Do not return as a code block.";
}
