using System.Text;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Shared.Documents.DocumentIntelligence;

/// <summary>
/// Defines a Markdown document converter that uses the Azure AI Document Intelligence service.
/// </summary>
/// <param name="client">The Document Intelligence client to use for conversion.</param>
/// <param name="logger">A logger for capturing diagnostic information.</param>
public class DocumentIntelligenceMarkdownConverter(
    DocumentIntelligenceClient client,
    ILogger<DocumentIntelligenceMarkdownConverter> logger)
    : IDocumentMarkdownConverter
{
    /// <inheritdoc />
    public Task<byte[]?> FromUriAsync(string documentUri, CancellationToken cancellationToken = default)
    {
        return ToMarkdownAsync(
            new AnalyzeDocumentContent { UrlSource = new Uri(documentUri) },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<byte[]?> FromByteArrayAsync(byte[] documentBytes, CancellationToken cancellationToken = default)
    {
        return ToMarkdownAsync(
            new AnalyzeDocumentContent { Base64Source = BinaryData.FromBytes(documentBytes) },
            cancellationToken);
    }

    private async Task<byte[]?> ToMarkdownAsync(
        AnalyzeDocumentContent content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-layout",
                content,
                outputContentFormat: ContentFormat.Markdown, cancellationToken: cancellationToken);

            if (operation is { HasValue: true })
            {
                return Encoding.UTF8.GetBytes(operation.Value.Content);
            }

            logger.LogError("Failed to analyze document content as Markdown.");
            return default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to analyze document content as Markdown.");
            return default;
        }
    }
}
