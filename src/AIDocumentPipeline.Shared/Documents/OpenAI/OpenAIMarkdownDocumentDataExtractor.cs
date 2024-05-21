using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIDocumentPipeline.Shared.Documents.OpenAI;

/// <summary>
/// Defines a document data extractor that combines Azure AI Document Intelligence and Azure OpenAI to extract structured data from Markdown documents.
/// </summary>
/// <param name="client">The Azure OpenAI client.</param>
/// <param name="markdownConverter">The document converter that converts documents to Markdown.</param>
/// <param name="options">The configuration options for the Azure OpenAI document data extraction service.</param>
/// <param name="logger">The observer for logging messages.</param>
public class OpenAIMarkdownDocumentDataExtractor(
    OpenAIClient client,
    IDocumentMarkdownConverter markdownConverter,
    IOptions<OpenAIDocumentDataExtractionOptions> options,
    ILogger<OpenAIMarkdownDocumentDataExtractor> logger)
    : IDocumentDataExtractor
{
    /// <inheritdoc />
    public async Task<T?> FromByteArrayAsync<T>(
        byte[] documentBytes,
        T schemaObject,
        Func<T, string> extractionPromptConstruct,
        CancellationToken cancellationToken = default) where T : class
    {
        var markdownContent = await markdownConverter.FromByteArrayAsync(documentBytes, cancellationToken);

        if (markdownContent != null)
        {
            return await FromContentAsync<T>(Encoding.UTF8.GetString(markdownContent), extractionPromptConstruct(schemaObject), cancellationToken);
        }

        logger.LogWarning("No Markdown content was returned from the document.");
        return default;
    }

    private async Task<T?> FromContentAsync<T>(
        string documentContent,
        string extractionPrompt,
        CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            var chatOptions = options.Value;

            AddSystemPrompt(options.Value.SystemPrompt, chatOptions.Messages);

            AddUserPrompt(extractionPrompt, chatOptions.Messages);
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
