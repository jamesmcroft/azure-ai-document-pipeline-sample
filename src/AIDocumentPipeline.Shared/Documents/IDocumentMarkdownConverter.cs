namespace AIDocumentPipeline.Shared.Documents;

/// <summary>
/// Defines an interface for converting documents to Markdown.
/// </summary>
public interface IDocumentMarkdownConverter
{
    /// <summary>
    /// Converts a document from a URI to a structured Markdown format.
    /// </summary>
    /// <param name="documentUri">The URI of the document to convert.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A structured Markdown representation of the document as a byte array.</returns>
    Task<byte[]?> FromUriAsync(string documentUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a document from a byte array to a structured Markdown format.
    /// </summary>
    /// <param name="documentBytes">The byte array of the document to convert.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A structured Markdown representation of the document as a byte array.</returns>
    Task<byte[]?> FromByteArrayAsync(byte[] documentBytes, CancellationToken cancellationToken = default);
}
