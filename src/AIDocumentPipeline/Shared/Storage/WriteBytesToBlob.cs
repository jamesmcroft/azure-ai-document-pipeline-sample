using AIDocumentPipeline.Shared.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Shared.Storage;

/// <summary>
/// Defines a collection of activities for interacting with Azure Blob Storage.
/// </summary>
/// <param name="storageClientFactory">The <see cref="AzureStorageClientFactory"/> instance used to interact with Azure Storage accounts.</param>
[ActivitySource]
public class WriteBytesToBlob(
    AzureStorageClientFactory storageClientFactory)
    : BaseActivity(Name)
{
    public const string Name = nameof(WriteBytesToBlob);

    /// <summary>
    /// Writes a byte array to a blob in Azure Storage.
    /// </summary>
    /// <param name="input">The blob storage information including the buffer byte array, storage account, container, and blob name.</param>
    /// <param name="context">The function execution context for execution-related functionality.</param>
    /// <exception cref="ArgumentException">Thrown when the input is invalid.</exception>
    [Function(Name)]
    public async Task<bool> RunAsync(
        [ActivityTrigger] Request input,
        FunctionContext context)
    {
        using var span = StartActiveSpan(Name, input);
        var logger = context.GetLogger(Name);

        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            logger.LogError("Invalid input: {ValidationErrors}", validationResult);
            return false;
        }

        var blobContainerClient = storageClientFactory
            .GetBlobServiceClient(input.StorageAccountName)
            .GetBlobContainerClient(input.ContainerName);

        await blobContainerClient.CreateIfNotExistsAsync();

        var blobClient = blobContainerClient.GetBlobClient(input.BlobName);

        using var stream = new MemoryStream(input.Content);
        await blobClient.UploadAsync(stream, overwrite: input.Overwrite);

        return true;
    }

    public class Request : BlobStorageRequest
    {
        public byte[] Content { get; set; } = [];

        public bool Overwrite { get; set; } = true;

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(StorageAccountName))
            {
                result.AddError($"{nameof(StorageAccountName)} is required.");
            }

            if (string.IsNullOrWhiteSpace(ContainerName))
            {
                result.AddError($"{nameof(ContainerName)} is required.");
            }

            if (string.IsNullOrWhiteSpace(BlobName))
            {
                result.AddError($"{nameof(BlobName)} is required.");
            }

            if (Content.Length == 0)
            {
                result.AddError($"{nameof(Content)} is required.");
            }

            return result;
        }
    }
}
