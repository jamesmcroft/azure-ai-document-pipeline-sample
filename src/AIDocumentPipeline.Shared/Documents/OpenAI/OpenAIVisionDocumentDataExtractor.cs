using System.Text.Json;
using System.Text.Json.Nodes;
using AIDocumentPipeline.Shared.Serialization.AzureOpenAI;
using Azure.AI.OpenAI;
using JsonSchemaMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using SkiaSharp;

namespace AIDocumentPipeline.Shared.Documents.OpenAI;

/// <summary>
/// Defines a document data extractor that uses Azure OpenAI to extract structured data using multi-modal vision capabilities.
/// </summary>
public class OpenAIVisionDocumentDataExtractor(
    AzureOpenAIClient client,
    IOptions<OpenAIDocumentDataExtractionOptions> options,
    ILogger<OpenAIVisionDocumentDataExtractor> logger)
    : IDocumentDataExtractor
{
    public async Task<T?> FromByteArrayAsync<T>(
        byte[] documentBytes,
        string extractionPrompt,
        CancellationToken cancellationToken = default) where T : class
    {
        var pageImages = ToProcessedImages(documentBytes);

        if (pageImages.Any())
        {
            try
            {
                var chatOptions = options.Value;

                AddSystemPrompt(options.Value.SystemPrompt, chatOptions.Messages);
                AddStructuredOutputSchema<T>(chatOptions);
                AddVisionPrompt(extractionPrompt, pageImages, chatOptions.Messages);

                var chatClient = client.GetChatClient(chatOptions.DeploymentName);

                var response = await chatClient.CompleteChatAsync<T?>(chatOptions.Messages, chatOptions, cancellationToken: cancellationToken);

                var parsedResult = response.Parsed;
                if (parsedResult != null)
                {
                    return parsedResult;
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

        logger.LogWarning("No images were returned from the document.");
        return default;
    }

    private void AddStructuredOutputSchema<T>(OpenAIDocumentDataExtractionOptions chatOptions) where T : class
    {
        chatOptions.ResponseFormat =
            typeof(T).CreateJsonSchemaFormat("document", "Output from document data extraction.", true);
    }

    private IEnumerable<byte[]> ToProcessedImages(byte[] documentBytes)
    {
        var pageImages = PDFtoImage.Conversion.ToImages(documentBytes);

        var totalPageCount = pageImages.Count();

        var maxSize = (int)Math.Ceiling(totalPageCount / 50.0);

        var pageImageGroups = new List<List<SKBitmap>>();

        for (var i = 0; i < totalPageCount; i += maxSize)
        {
            var pageImageGroup = pageImages.Skip(i).Take(maxSize).ToList();
            pageImageGroups.Add(pageImageGroup);
        }

        var pdfImageFiles = new List<byte[]>();

        foreach (var pageImageGroup in pageImageGroups)
        {
            var totalHeight = pageImageGroup.Sum(image => image.Height);
            var width = pageImageGroup.Max(image => image.Width);

            var stitchedImage = new SKBitmap(width, totalHeight);
            var canvas = new SKCanvas(stitchedImage);
            var currentHeight = 0;
            foreach (var pageImage in pageImageGroup)
            {
                canvas.DrawBitmap(pageImage, 0, currentHeight);
                currentHeight += pageImage.Height;
            }

            var stitchedImageStream = new MemoryStream();
            stitchedImage.Encode(stitchedImageStream, SKEncodedImageFormat.Jpeg, 100);
            pdfImageFiles.Add(stitchedImageStream.ToArray());
        }

        return pdfImageFiles;
    }

    private static void AddSystemPrompt(string systemPrompt, ICollection<ChatMessage> messages)
    {
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new SystemChatMessage(systemPrompt));
        }
    }

    private static void AddVisionPrompt(
        string userPrompt,
        IEnumerable<byte[]> pageImages,
        ICollection<ChatMessage> messages)
    {
        if (string.IsNullOrEmpty(userPrompt))
        {
            return;
        }

        var userPromptParts = new List<ChatMessageContentPart> { ChatMessageContentPart.CreateTextPart(userPrompt) };
        userPromptParts.AddRange(pageImages.Select(image =>
            ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(image), "image/jpeg")));

        messages.Add(new UserChatMessage(userPromptParts.ToArray()));
    }
}
