using Azure.Storage.Blobs;

namespace AIDocumentPipeline.Shared.Storage;

/// <summary>
/// Defines a collection of extension methods for working with Azure Blob Storage.
/// </summary>
public static class BlobExtensions
{
    /// <summary>
    /// Retrieves a list of blob names grouped by their root folder.
    /// </summary>
    /// <param name="containerClient">The <see cref="BlobContainerClient"/> to retrieve blobs from.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The list of blob names grouped by their root folder.</returns>
    public static async Task<List<IGrouping<string, string>>> GetBlobsByRootFolderAsync(
        this BlobContainerClient containerClient,
        CancellationToken cancellationToken = default)
    {
        var blobNames = new List<string>();

        string? continuationToken = null;

        do
        {
            await foreach (var page in containerClient.GetBlobsAsync(cancellationToken: cancellationToken).AsPages(continuationToken).WithCancellation(cancellationToken))
            {
                blobNames.AddRange(page.Values.Select(blobItem => blobItem.Name));
            }
        } while (!string.IsNullOrEmpty(continuationToken));

        return blobNames.GroupBy(blobName => blobName.Split('/')[0]).ToList();
    }
}
