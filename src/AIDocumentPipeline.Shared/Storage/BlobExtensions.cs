using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace AIDocumentPipeline.Shared.Storage;

/// <summary>
/// Defines a collection of extension methods for working with Azure Blob Storage.
/// </summary>
public static class BlobExtensions
{
    /// <summary>
    /// Retrieves a list of blob names grouped by the folder at the root of the container.
    /// </summary>
    /// <remarks>
    /// Any blobs in the root of the container are grouped into a folder named after the container.
    /// </remarks>
    /// <param name="containerClient">The <see cref="BlobContainerClient"/> to retrieve blobs from.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The groupings of blobs by their folder at the root of the container.</returns>
    public static async Task<List<IGrouping<string, string>>> GetBlobsByFolderAtRootAsync(
        this BlobContainerClient containerClient,
        CancellationToken cancellationToken = default)
    {
        var blobNames = new List<string>();

        string? continuationToken = null;

        do
        {
            await foreach (var page in containerClient
                               .GetBlobsAsync(cancellationToken: cancellationToken)
                               .AsPages(continuationToken)
                               .WithCancellation(cancellationToken))
            {
                blobNames.AddRange(page.Values.Select(blobItem => blobItem.Name));
            }
        } while (!string.IsNullOrEmpty(continuationToken));

        if (blobNames.Any(blobName => !blobName.Contains('/')))
        {
            // For any blobs in the root of the container, group into a folder named after the container.
            blobNames = blobNames.Select(blobName =>
                blobName.Contains('/') ? blobName : $"{containerClient.Name}/" + blobName).ToList();
        }

        return blobNames.GroupBy(blobName => blobName.Split('/')[0]).ToList();
    }

    /// <summary>
    /// Generates a shared access signature (SAS) URI for specific blob in a specific container.
    /// </summary>
    /// <param name="clientFactory">The <see cref="AzureStorageClientFactory"/> to use for creating the SAS URI.</param>
    /// <param name="storageAccountName">The name of the storage account to generate the SAS URI for.</param>
    /// <param name="containerName">The name of the container containing the blob.</param>
    /// <param name="blobName">The name of the blob to generate the SAS URI for.</param>
    /// <param name="permissions">The permissions to grant in the SAS URI. Default is <see cref="BlobSasPermissions.Read"/>.</param>
    /// <param name="expiresIn">The number of hours until the SAS URI expires. Default is 1 hour.</param>
    /// <returns>The generated SAS URI for the specified blob.</returns>
    public static async Task<string> GenerateBlobSasUriAsync(
        this AzureStorageClientFactory clientFactory,
        string storageAccountName,
        string containerName,
        string blobName,
        BlobSasPermissions permissions = BlobSasPermissions.Read,
        int expiresIn = 1)
    {
        var blobServiceClient = clientFactory
            .GetBlobServiceClient(storageAccountName);

        var blobClient = blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        if (!blobClient.CanGenerateSasUri)
        {
            // The blob client is constructed with managed identity and cannot generate SAS tokens, so we need to create one manually.
            var userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(expiresIn));
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(expiresIn)
            };
            sasBuilder.SetPermissions(permissions);

            var blobUriBuilder = new BlobUriBuilder(blobClient.Uri)
            {
                Sas = sasBuilder.ToSasQueryParameters(userDelegationKey, blobServiceClient.AccountName)
            };

            return blobUriBuilder.ToUri().ToString();
        }

        var sasUri = blobClient.GenerateSasUri(permissions, DateTimeOffset.UtcNow.AddHours(expiresIn));
        return sasUri.ToString();
    }
}
