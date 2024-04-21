using AIDocumentPipeline.Invoices.Activities;
using AIDocumentPipeline.Shared;
using AIDocumentPipeline.Shared.Observability;
using AIDocumentPipeline.Shared.Storage;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Invoices;

[ActivitySource]
public class ExtractInvoiceDataWorkflow(
    InvoicesSettings settings)
    : BaseWorkflow(Name)
{
    public const string Name = nameof(ExtractInvoiceDataWorkflow);

    [Function(Name)]
    public async Task<WorkflowResult> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // Step 1: Extract the input from the context.
        var input = context.GetInput<InvoiceFolder>() ??
                    throw new ArgumentException(
                        $"{nameof(InvoiceFolder)} is required to start the workflow.",
                        nameof(context));

        using var span = StartActiveSpan(Name, input);
        var logger = context.CreateReplaySafeLogger(Name);

        var result = new WorkflowResult { WorkflowName = Name };

        // Step 2: Validate the input.
        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            result.Merge(
                result,
                nameof(InvoiceFolder.Validate),
                $"{nameof(input)} is invalid.",
                logger,
                LogLevel.Error);
            return result;
        }

        result.Add(nameof(InvoiceFolder.Validate), $"{nameof(input)} is valid.", logger);

        // Step 3: Process each invoice file.
        foreach (var invoice in input.InvoiceFileNames)
        {
            var invoiceMarkdown = await CallActivityAsync<byte[]?>(
                context,
                GetInvoiceMarkdown.Name,
                new GetInvoiceMarkdown.Request { Container = input.Container, FileName = invoice },
                span.Context);

            if (invoiceMarkdown is null)
            {
                result.AddError(
                    GetInvoiceMarkdown.Name,
                    $"Failed to get the markdown for {invoice}.",
                    logger,
                    LogLevel.Error);
                continue;
            }

            var invoiceData = await CallActivityAsync<InvoiceData?>(
                context,
                ExtractInvoiceData.Name,
                new ExtractInvoiceData.Request { Markdown = invoiceMarkdown },
                span.Context);

            if (invoiceData is null)
            {
                result.AddError(
                    ExtractInvoiceData.Name,
                    $"Failed to extract data from the markdown for {invoice}.",
                    logger,
                    LogLevel.Error);
                continue;
            }

            var invoiceDataStored = await CallActivityAsync<bool>(
                context,
                WriteBytesToBlob.Name,
                new WriteBytesToBlob.Request
                {
                    StorageAccountName = settings.InvoicesStorageAccountName,
                    ContainerName = input.Container!,
                    BlobName = $"{invoice}.ExtractedData.json",
                    Content = JsonSerializer.SerializeToUtf8Bytes(invoiceData)
                },
                span.Context);

            if (!invoiceDataStored)
            {
                result.AddError(
                    WriteBytesToBlob.Name,
                    $"Failed to store the extracted data for {invoice}.",
                    logger,
                    LogLevel.Error);
                continue;
            }

            var invoiceDataValidation = await CallActivityAsync<ValidateInvoiceData.Result>(
                context,
                ValidateInvoiceData.Name,
                new ValidateInvoiceData.Request { Data = invoiceData },
                span.Context);

            result.Merge(
                invoiceDataValidation,
                ValidateInvoiceData.Name,
                $"Validated the extracted data for {invoice}.",
                logger);
        }

        return result;
    }
}
