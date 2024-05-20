using AIDocumentPipeline.Shared.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;

namespace AIDocumentPipeline.Workflows;

[ActivitySource]
public class WorkflowStatusFunctions
{
    public static string GetInstanceUrl(string instanceId) => $"/workflow/{instanceId}/status";

    [Function(nameof(GetWorkflowStatusesAsync))]
    public async Task<Page<OrchestrationMetadata>?> GetWorkflowStatusesAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "workflow/status")]
        HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        var continuationToken = req.Query["continuationToken"];

        var items = client.GetAllInstancesAsync(new OrchestrationQuery(ContinuationToken: continuationToken)).AsPages();
        await foreach (var page in items)
        {
            return page;
        }

        return default;
    }

    [Function(nameof(ClearWorkflowStatusesAsync))]
    public async Task<PurgeResult?> ClearWorkflowStatusesAsync(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "workflow/status")]
        HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        return await client.PurgeAllInstancesAsync(new PurgeInstancesFilter(Statuses: new List<OrchestrationRuntimeStatus>
        {
            OrchestrationRuntimeStatus.Completed,
            OrchestrationRuntimeStatus.Failed,
            OrchestrationRuntimeStatus.Terminated
        }));
    }

    [Function(nameof(GetWorkflowStatus))]
    public async Task<OrchestrationMetadata?> GetWorkflowStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "workflow/{instanceId}/status")]
        HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        return await client.GetInstanceAsync(instanceId);
    }

    [Function(nameof(ResumeWorkflow))]
    public async Task<OrchestrationMetadata?> ResumeWorkflow(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "workflow/{instanceId}/resume")]
        HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        var instance = await client.GetInstanceAsync(instanceId);

        switch (instance)
        {
            case null:
                throw new InvalidOperationException($"Instance {instanceId} not found");
            default:
                switch (instance.RuntimeStatus)
                {
                    case OrchestrationRuntimeStatus.Suspended:
                        await client.ResumeInstanceAsync(instanceId, "Resumed by user");
                        break;
                    case OrchestrationRuntimeStatus.Running:
                    case OrchestrationRuntimeStatus.Completed:
                    case OrchestrationRuntimeStatus.Failed:
                    case OrchestrationRuntimeStatus.Terminated:
                    case OrchestrationRuntimeStatus.Pending:
                    default:
                        throw new InvalidOperationException($"Instance {instanceId} is not in a valid state to start. Current state: {instance.RuntimeStatus}");
                }

                return await client.GetInstanceAsync(instanceId);
        }
    }

    [Function(nameof(PauseWorkflow))]
    public async Task<OrchestrationMetadata?> PauseWorkflow(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "workflow/{instanceId}/pause")]
        HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        var instance = await client.GetInstanceAsync(instanceId);

        switch (instance)
        {
            case null:
                throw new InvalidOperationException($"Instance {instanceId} not found");
            default:
                switch (instance.RuntimeStatus)
                {
                    case OrchestrationRuntimeStatus.Running:
                        await client.SuspendInstanceAsync(instanceId, "Stopped by user");
                        break;
                    case OrchestrationRuntimeStatus.Suspended:
                    case OrchestrationRuntimeStatus.Completed:
                    case OrchestrationRuntimeStatus.Failed:
                    case OrchestrationRuntimeStatus.Terminated:
                    case OrchestrationRuntimeStatus.Pending:
                    default:
                        throw new InvalidOperationException($"Instance {instanceId} is not in a valid state to stop. Current state: {instance.RuntimeStatus}");
                }

                return await client.GetInstanceAsync(instanceId);
        }
    }

    [Function(nameof(TerminateWorkflow))]
    public async Task<OrchestrationMetadata?> TerminateWorkflow(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "workflow/{instanceId}/terminate")]
        HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        var instance = await client.GetInstanceAsync(instanceId);

        switch (instance)
        {
            case null:
                throw new InvalidOperationException($"Instance {instanceId} not found");
            default:
                switch (instance.RuntimeStatus)
                {
                    case OrchestrationRuntimeStatus.Running:
                    case OrchestrationRuntimeStatus.Suspended:
                    case OrchestrationRuntimeStatus.Pending:
                        await client.TerminateInstanceAsync(instanceId, "Terminated by user");
                        break;
                    case OrchestrationRuntimeStatus.Completed:
                    case OrchestrationRuntimeStatus.Failed:
                    case OrchestrationRuntimeStatus.Terminated:
                    default:
                        throw new InvalidOperationException($"Instance {instanceId} is not in a valid state for terminating. Current state: {instance.RuntimeStatus}");
                }

                return await client.GetInstanceAsync(instanceId);
        }
    }

    [Function(nameof(ClearWorkflowStatusAsync))]
    public async Task<PurgeResult?> ClearWorkflowStatusAsync(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "workflow/{instanceId}/status")]
        HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        return await client.PurgeInstanceAsync(instanceId);
    }
}
