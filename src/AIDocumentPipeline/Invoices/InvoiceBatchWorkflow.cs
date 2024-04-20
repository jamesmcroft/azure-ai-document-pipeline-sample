using AIDocumentPipeline.Shared;
using AIDocumentPipeline.Shared.Documents;
using AIDocumentPipeline.Shared.Observability;
using AIDocumentPipeline.Shared.Storage;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Invoices;

[ActivitySource]
public class InvoiceBatchWorkflow(
    IDocumentMarkdownConverter documentConverter,
    IDocumentDataExtractor documentDataExtractor,
    AzureStorageClientFactory storageClientFactory,
    InvoicesSettings settings)
    : BaseWorkflow(WorkflowName)
{
    private const string WorkflowName = nameof(InvoiceBatchWorkflow);

    [Function(nameof(ProcessInvoiceBatchHttp))]
    public async Task<HttpResponseData> ProcessInvoiceBatchHttp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices")]
        HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient,
        FunctionContext context)
    {
        using var span = StartActiveSpan(nameof(ProcessInvoiceBatchHttp));
        var log = context.GetLogger(nameof(ProcessInvoiceBatchHttp));

        var requestBody = await req.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            throw new ArgumentException("Request body is required.", nameof(req));
        }

        var instanceId = await StartWorkflowAsync(
            durableClient,
            ExtractInput<InvoiceBatchRequest>(requestBody),
            span.Context);

        log.LogInformation("Started workflow with instance ID: {InstanceId}", instanceId);

        return await durableClient.CreateCheckStatusResponseAsync(req, instanceId);
    }

    [Function(nameof(ProcessInvoiceBatchQueue))]
    public async Task ProcessInvoiceBatchQueue(
        [QueueTrigger("invoices", Connection = InvoicesSettings.InvoicesQueueConnectionConfigKey)]
        InvoiceBatchRequest? request,
        [DurableClient] DurableTaskClient durableClient,
        FunctionContext context)
    {
        using var span = StartActiveSpan(nameof(ProcessInvoiceBatchQueue));
        var log = context.GetLogger(nameof(ProcessInvoiceBatchQueue));

        if (request is null)
        {
            throw new ArgumentException($"{nameof(InvoiceBatchRequest)} is required.", nameof(request));
        }

        var instanceId = await StartWorkflowAsync(
            durableClient,
            request,
            span.Context);

        log.LogInformation("Started workflow with instance ID: {InstanceId}", instanceId);
    }

    [Function(WorkflowName)]
    public async Task<List<string>> ExecuteInvoicesWorkflow(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // Step 1: Extract the input from the context.
        var input = context.GetInput<InvoiceBatchRequest>() ??
                    throw new ArgumentException(
                        $"{nameof(InvoiceBatchRequest)} is required to start the workflow.",
                        nameof(context));

        using var span = StartActiveSpan(nameof(ExecuteInvoicesWorkflow), input);
        var log = context.CreateReplaySafeLogger(nameof(ExecuteInvoicesWorkflow));

        var workflowResult = new WorkflowResult(WorkflowName, log);

        // Step 2: Validate the input.
        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            workflowResult.AddRange(
                nameof(InvoiceBatchRequest.Validate),
                $"{nameof(input)} is invalid.",
                validationResult.Messages,
                LogLevel.Error);
            return workflowResult.Messages;
        }

        // Step 3: Get the invoice folders from the blob container.
        var invoiceFolders = await CallActivityAsync<List<InvoiceFolder>>(
            context,
            nameof(GetInvoiceFoldersActivity),
            input,
            span.Context);

        // Step 4: Process the invoices in each folder.

        return workflowResult.Messages;
    }

    [Function(nameof(GetInvoiceFoldersActivity))]
    public async Task<List<InvoiceFolder>> GetInvoiceFoldersActivity(
        [ActivityTrigger] InvoiceBatchRequest input,
        FunctionContext context)
    {
        using var span = StartActiveSpan(nameof(GetInvoiceFoldersActivity), input);
        var log = context.GetLogger(nameof(GetInvoiceFoldersActivity));

        var groupedInvoices = await storageClientFactory
            .GetBlobServiceClient(settings.InvoicesStorageAccountName)
            .GetBlobContainerClient(input.Container)
            .GetBlobsByRootFolderAsync();

        log.LogInformation("Found {InvoiceFolderCount} invoice folders in the container.", groupedInvoices.Count);

        return groupedInvoices
            .Select(group => new InvoiceFolder { Name = group.Key, InvoiceFileNames = group.ToList() }).ToList();
    }

    /// <summary>
    /// Defines a request object for processing a batch of invoices from a blob container.
    /// </summary>
    public class InvoiceBatchRequest : BaseWorkflowRequest
    {
        /// <summary>
        /// Gets or sets the name of the blob container which contains the invoices.
        /// </summary>
        public string? Container { get; set; }

        /// <inheritdoc />
        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(Container))
            {
                result.AddError($"{nameof(Container)} is required.");
            }

            return result;
        }
    }
}
