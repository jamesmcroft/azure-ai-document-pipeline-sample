using AIDocumentPipeline.Shared;
using AIDocumentPipeline.Shared.Observability;
using AIDocumentPipeline.Shared.Storage;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Invoices.Activities;

[ActivitySource]
public class GetInvoiceFolders(
    AzureStorageClientFactory storageClientFactory,
    InvoicesSettings settings)
    : BaseWorkflow(Name)
{
    public const string Name = nameof(GetInvoiceFolders);

    [Function(Name)]
    public async Task<List<InvoiceFolder>> RunAsync(
        [ActivityTrigger] InvoiceBatchRequest input,
        FunctionContext context)
    {
        using var span = StartActiveSpan(Name, input);
        var logger = context.GetLogger(Name);

        var groupedInvoices = await storageClientFactory
            .GetBlobServiceClient(settings.InvoicesStorageAccountName)
            .GetBlobContainerClient(input.Container)
            .GetBlobsByFolderAtRootAsync();

        logger.LogInformation("Found {InvoiceFolderCount} invoice folders in the container.", groupedInvoices.Count);

        return groupedInvoices
            .Select(group =>
                new InvoiceFolder { Container = input.Container, Name = group.Key, InvoiceFileNames = group.ToList() })
            .ToList();
    }
}
