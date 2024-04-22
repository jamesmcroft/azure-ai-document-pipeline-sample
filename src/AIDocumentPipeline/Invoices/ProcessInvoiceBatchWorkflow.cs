using AIDocumentPipeline.Invoices.Activities;
using AIDocumentPipeline.Shared;
using AIDocumentPipeline.Shared.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Invoices;

[ActivitySource]
public class ProcessInvoiceBatchWorkflow()
    : BaseWorkflow(Name)
{
    private const string Name = nameof(ProcessInvoiceBatchWorkflow);

    [Function(nameof(ProcessInvoiceBatchHttp))]
    public async Task<HttpResponseData> ProcessInvoiceBatchHttp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invoices")]
        HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient,
        FunctionContext context)
    {
        using var span = StartActiveSpan(nameof(ProcessInvoiceBatchHttp));
        var logger = context.GetLogger(nameof(ProcessInvoiceBatchHttp));

        var requestBody = await req.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            throw new ArgumentException("Request body is required.", nameof(req));
        }

        var instanceId = await StartWorkflowAsync(
            durableClient,
            ExtractInput<InvoiceBatchRequest>(requestBody),
            span.Context);

        logger.LogInformation("Started workflow with instance ID: {InstanceId}", instanceId);

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
        var logger = context.GetLogger(nameof(ProcessInvoiceBatchQueue));

        if (request is null)
        {
            throw new ArgumentException($"{nameof(InvoiceBatchRequest)} is required.", nameof(request));
        }

        var instanceId = await StartWorkflowAsync(
            durableClient,
            request,
            span.Context);

        logger.LogInformation("Started workflow with instance ID: {InstanceId}", instanceId);
    }

    [Function(Name)]
    public async Task<WorkflowResult> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // Step 1: Extract the input from the context.
        var input = context.GetInput<InvoiceBatchRequest>() ??
                    throw new ArgumentException(
                        $"{nameof(InvoiceBatchRequest)} is required to start the workflow.",
                        nameof(context));

        using var span = StartActiveSpan(Name, input);
        var logger = context.CreateReplaySafeLogger(Name);

        var result = new WorkflowResult { Name = Name };

        // Step 2: Validate the input.
        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            result.Merge(validationResult);
            return result;
        }

        result.AddMessage(nameof(InvoiceBatchRequest.Validate), $"{nameof(input)} is valid.", logger);

        // Step 3: Get the invoice folders from the blob container.
        var invoiceFolders = await CallActivityAsync<List<InvoiceFolder>>(
            context,
            GetInvoiceFolders.Name,
            input,
            span.Context);

        result.AddMessage(GetInvoiceFolders.Name, $"Retrieved {invoiceFolders.Count} invoice folders.", logger);

        // Step 4: Process the invoices in each folder.
        var extractInvoiceDataTasks = invoiceFolders.Where(folder => folder.Name != input.Container).Select(folder =>
                CallWorkflowAsync<WorkflowResult>(context, ExtractInvoiceDataWorkflow.Name, folder, span.Context))
            .ToList();

        await Task.WhenAll(extractInvoiceDataTasks);

        foreach (var task in extractInvoiceDataTasks)
        {
            result.AddActivityResult(ExtractInvoiceDataWorkflow.Name, "Processed invoice folder.", task.Result, logger);
        }

        return result;
    }
}
