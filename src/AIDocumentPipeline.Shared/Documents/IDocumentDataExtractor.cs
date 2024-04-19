namespace AIDocumentPipeline.Shared.Documents;

/// <summary>
/// Defines an interface for extracting data from a document.
/// </summary>
public interface IDocumentDataExtractor
{
    /// <summary>
    /// Extracts data from the specified document content.
    /// </summary>
    /// <typeparam name="T">The type of data to extract.</typeparam>
    /// <param name="documentContent">The content of the document to extract data from.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The extracted data, or <see langword="null"/> if the data could not be extracted.</returns>
    Task<T?> FromContentAsync<T>(string documentContent, CancellationToken cancellationToken = default)
        where T : class;
}
