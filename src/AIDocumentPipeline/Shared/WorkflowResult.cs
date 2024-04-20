using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Shared;

/// <summary>
/// Defines a model that represents the result of a workflow.
/// </summary>
/// <param name="workflowName">The name of the workflow.</param>
/// <param name="workflowLogger">The logger to use for logging workflow results.</param>
public class WorkflowResult(string workflowName, ILogger? workflowLogger = default)
{
    private const string ResultMessageFormat = "{WorkflowName}::{WorkflowAction} - {WorkflowActionMessage}";

    /// <summary>
    /// Gets the results of the workflow.
    /// </summary>
    public List<string> Messages { get; } = new();

    /// <summary>
    /// Adds a result to the workflow.
    /// </summary>
    /// <param name="action">The action that was performed.</param>
    /// <param name="message">The message to add to the results.</param>
    /// <param name="logLevel">The log level to use when logging the result.</param>
    public void Add(string action, string message, LogLevel logLevel = LogLevel.Information)
    {
        Messages.Add($"{workflowName}::{action} - {message}");
        workflowLogger?.Log(logLevel, ResultMessageFormat, workflowName, action, message);
    }

    /// <summary>
    /// Adds a range of results to the workflow.
    /// </summary>
    /// <param name="action">The action that was performed.</param>
    /// <param name="message">The message to add to the log.</param>
    /// <param name="results">The results to add.</param>
    /// <param name="logLevel">The log level to use when logging the result.</param>
    public void AddRange(
        string action,
        string message,
        IEnumerable<string> results,
        LogLevel logLevel = LogLevel.Information)
    {
        Messages.AddRange(results);
        workflowLogger?.Log(logLevel, ResultMessageFormat, workflowName, action, message);
    }
}
