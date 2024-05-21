using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace AIDocumentPipeline.Shared.Documents.OpenAI;

/// <summary>
/// Defines a document data extractor that uses Azure OpenAI to extract structured data using multi-modal vision capabilities.
/// </summary>
public class OpenAIVisionDocumentDataExtractor(
    OpenAIClient client,
    IOptions<OpenAIDocumentDataExtractionOptions> options,
    ILogger<OpenAIVisionDocumentDataExtractor> logger)
    : IDocumentDataExtractor
{
    /// <inheritdoc />
    public async Task<T?> FromByteArrayAsync<T>(
        byte[] documentBytes,
        T schemaObject,
        Func<T, string> extractionPromptConstruct,
        CancellationToken cancellationToken = default) where T : class
    {
        var pageImages = ToProcessedImages(documentBytes);

        if (pageImages.Any())
        {
            try
            {
                var chatOptions = options.Value;

                AddSystemPrompt(options.Value.SystemPrompt, chatOptions.Messages);

                AddVisionPrompt(extractionPromptConstruct(schemaObject), pageImages, chatOptions.Messages);

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

        logger.LogWarning("No images were returned from the document.");
        return default;
    }

    private IEnumerable<byte[]> ToProcessedImages(byte[] documentBytes)
    {
        var pageImages = PDFtoImage.Conversion.ToImages(documentBytes);

        var totalPageCount = pageImages.Count();

        // If there are more than 10 pages, we need to stitch images together so that the total number of pages is less than or equal to 10 for the OpenAI API.
        var maxSize = (int)Math.Ceiling(totalPageCount / 10.0);

        var pageImageGroups = new List<List<SKBitmap>>();

        for (var i = 0; i < totalPageCount; i += maxSize)
        {
            var pageImageGroup = pageImages.Skip(i).Take(maxSize).ToList();
            pageImageGroups.Add(pageImageGroup);
        }

        var pdfImageFiles = new List<byte[]>();

        // Stitch images together if they have been grouped. This should result in a total of 10 or fewer images in the list.
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

            //stitchedImage = stitchedImage.Resize(new SKImageInfo(width * 2, totalHeight * 2), SKFilterQuality.High);

            var stitchedImageStream = new MemoryStream();
            stitchedImage.Encode(stitchedImageStream, SKEncodedImageFormat.Jpeg, 100);
            pdfImageFiles.Add(stitchedImageStream.ToArray());
        }

        return pdfImageFiles;
    }

    private static void AddSystemPrompt(string systemPrompt, ICollection<ChatRequestMessage> messages)
    {
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new ChatRequestSystemMessage(systemPrompt));
        }
    }

    private static void AddVisionPrompt(string userPrompt, IEnumerable<byte[]> pageImages,
        ICollection<ChatRequestMessage> messages)
    {
        if (!string.IsNullOrEmpty(userPrompt))
        {
            var userPromptParts = new List<ChatMessageContentItem> { new ChatMessageTextContentItem(userPrompt) };
            userPromptParts.AddRange(pageImages.Select(image =>
                new ChatMessageImageContentItem(BinaryData.FromBytes(image), "image/jpeg")));

            messages.Add(new ChatRequestUserMessage(userPromptParts.ToArray()));
        }
    }
}
