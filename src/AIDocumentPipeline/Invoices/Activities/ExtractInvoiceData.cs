using AIDocumentPipeline.Shared;
using AIDocumentPipeline.Shared.Documents;
using AIDocumentPipeline.Shared.Observability;
using AIDocumentPipeline.Shared.Storage;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Invoices.Activities;

[ActivitySource]
public class ExtractInvoiceData(
    IDocumentDataExtractor documentDataExtractor,
    AzureStorageClientFactory storageClientFactory,
    InvoicesSettings settings)
    : BaseActivity(Name)
{
    public const string Name = nameof(ExtractInvoiceData);

    [Function(Name)]
    public async Task<InvoiceData?> RunAsync(
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

        await using var blobContentStream = await storageClientFactory.GetBlobContentAsync(
            settings.InvoicesStorageAccountName,
            input.Container!,
            input.FileName!);

        // ToDo, experiment with the extraction prompt below to tailor it to the needs of your specific documents.
        // Note, treat your prompt like any versioned code. Changes alter the responses generated by the model from previous iterations.

        return await documentDataExtractor.FromByteArrayAsync(
            blobContentStream.ToArray(),
            InvoiceData.Empty,
            i => $"Extract all the data from the pages. If a value is not present, provide null. Do not make up values if they do not exist. Use the following JSON schema: {JsonSerializer.Serialize(i)}");
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
