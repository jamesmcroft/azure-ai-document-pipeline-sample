namespace AIDocumentPipeline.Shared.Storage;

public abstract class BlobStorageRequest : BaseWorkflowRequest
{
    public string StorageAccountName { get; set; } = string.Empty;

    public string ContainerName { get; set; } = string.Empty;

    public string BlobName { get; set; } = string.Empty;
}
