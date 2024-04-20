using Azure.Storage.Blobs;

namespace AIDocumentPipeline.Shared.Storage;

public static class BlobExtensions
{
    public static async Task<IEnumerable<IGrouping<string, string>>> GetBlobsByRootFolderAsync(
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

        return blobNames.GroupBy(blobName => blobName.Split('/')[0]);
    }
}
