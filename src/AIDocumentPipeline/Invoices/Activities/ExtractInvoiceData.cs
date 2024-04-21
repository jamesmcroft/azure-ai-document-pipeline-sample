using System.Text;
using AIDocumentPipeline.Shared;
using AIDocumentPipeline.Shared.Documents;
using AIDocumentPipeline.Shared.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Invoices.Activities;

[ActivitySource]
public class ExtractInvoiceData(
    IDocumentDataExtractor documentDataExtractor)
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

        return await documentDataExtractor.FromContentAsync(
            Encoding.UTF8.GetString(input.Markdown!),
            InvoiceData.Empty);
    }

    public class Request : BaseWorkflowRequest
    {
        public byte[]? Markdown { get; set; }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (Markdown is null || Markdown.Length == 0)
            {
                result.AddError($"{nameof(Markdown)} is required.");
            }

            return result;
        }
    }
}
