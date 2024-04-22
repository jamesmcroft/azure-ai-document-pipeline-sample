using AIDocumentPipeline.Shared;
using AIDocumentPipeline.Shared.Documents;
using AIDocumentPipeline.Shared.Observability;
using AIDocumentPipeline.Shared.Storage;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Invoices.Activities;

[ActivitySource]
public class GetInvoiceMarkdown(
    IDocumentMarkdownConverter documentMarkdownConverter,
    AzureStorageClientFactory storageClientFactory,
    InvoicesSettings settings)
    : BaseActivity(Name)
{
    public const string Name = nameof(GetInvoiceMarkdown);

    [Function(Name)]
    public async Task<byte[]?> RunAsync(
        [ActivityTrigger] Request input,
        FunctionContext context)
    {
        using var span = StartActiveSpan(Name, input);
        var logger = context.GetLogger(Name);

        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            logger.LogError("Invalid input: {ValidationErrors}", validationResult);
            return null;
        }

        var blobUri = await storageClientFactory.GenerateBlobSasUriAsync(
            settings.InvoicesStorageAccountName,
            input.Container!,
            input.FileName!);

        return await documentMarkdownConverter.FromUriAsync(blobUri);
    }

    public class Request : BaseWorkflowRequest
    {
        public string? Container { get; set; }

        public string? FileName { get; set; }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(Container))
            {
                result.AddError($"{nameof(Container)} is required.");
            }

            if (string.IsNullOrWhiteSpace(FileName))
            {
                result.AddError($"{nameof(FileName)} is required.");
            }

            return result;
        }
    }
}