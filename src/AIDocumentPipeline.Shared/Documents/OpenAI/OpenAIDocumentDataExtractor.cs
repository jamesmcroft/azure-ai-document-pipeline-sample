using System.Globalization;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIDocumentPipeline.Shared.Documents.OpenAI;

/// <summary>
/// Defines a document data extractor that uses the Azure OpenAI service.
/// </summary>
/// <param name="client">The Azure OpenAI client.</param>
/// <param name="options">The configuration options for the Azure OpenAI document data extraction service.</param>
/// <param name="logger">The observer for logging messages.</param>
public class OpenAIDocumentDataExtractor(
    OpenAIClient client,
    IOptions<OpenAIDocumentDataExtractionOptions> options,
    ILogger<OpenAIDocumentDataExtractor> logger)
    : IDocumentDataExtractor
{
    private const string ExtractDataPromptFormat =
        "Extract the data from this document. If a value is not present, provide null. Use the following JSON schema: {0}";

    /// <inheritdoc />
    public async Task<T?> FromContentAsync<T>(
        string documentContent,
        T schemaObject,
        CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            var chatOptions = options.Value;

            AddSystemPrompt(options.Value.SystemPrompt, chatOptions.Messages);

            var extractionMessage = string.Format(CultureInfo.InvariantCulture, ExtractDataPromptFormat, JsonSerializer.Serialize(schemaObject));
            AddUserPrompt(extractionMessage, chatOptions.Messages);
            AddUserPrompt(documentContent, chatOptions.Messages);

            var response = await client.GetChatCompletionsAsync(chatOptions, cancellationToken);

            var completion = response.Value.Choices[0];
            if (completion == null)
            {
                logger.LogWarning("No data was returned from the Azure OpenAI service.");
                return null;
            }

            var extractedData = completion.Message.Content;
            if (!string.IsNullOrEmpty(extractedData))
            {
                return JsonSerializer.Deserialize<T>(extractedData);
            }

            logger.LogWarning("No data was extracted from the document.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to extract data from the document. {Error}", ex.Message);
            throw;
        }
    }

    private static void AddSystemPrompt(string systemPrompt, ICollection<ChatRequestMessage> messages)
    {
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new ChatRequestSystemMessage(systemPrompt));
        }
    }

    private static void AddUserPrompt(string userPrompt, ICollection<ChatRequestMessage> messages)
    {
        if (!string.IsNullOrEmpty(userPrompt))
        {
            messages.Add(new ChatRequestUserMessage(userPrompt));
        }
    }
}
