using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Shared;

/// <summary>
/// Defines a model that represents the result of a workflow.
/// </summary>
public class WorkflowResult : ValidationResult
{
    private const string ResultMessageFormat = "{WorkflowName}::{WorkflowAction} - {WorkflowActionMessage}";

    /// <summary>
    /// Gets or sets the name of the workflow.
    /// </summary>
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// Adds a result to the workflow.
    /// </summary>
    /// <param name="action">The action that was performed.</param>
    /// <param name="message">The message to add to the results.</param>
    /// <param name="logger">The logger to use for logging the result.</param>
    /// <param name="logLevel">The log level to use when logging the result.</param>
    public void Add(string action, string message, ILogger? logger = default, LogLevel logLevel = LogLevel.Information)
    {
        Add($"{WorkflowName}::{action} - {message}");
        logger?.Log(logLevel, ResultMessageFormat, WorkflowName, action, message);
    }

    /// <summary>
    /// Adds a range of results to the workflow.
    /// </summary>
    /// <param name="action">The action that was performed.</param>
    /// <param name="message">The message to add to the log.</param>
    /// <param name="results">The results to add.</param>
    /// <param name="logger">The logger to use for logging the result.</param>
    /// <param name="logLevel">The log level to use when logging the result.</param>
    public void AddRange(
        string action,
        string message,
        IEnumerable<string> results,
        ILogger? logger = default,
        LogLevel logLevel = LogLevel.Information)
    {
        AddRange(results);
        logger?.Log(logLevel, ResultMessageFormat, WorkflowName, action, message);
    }

    public void AddError(
        string action,
        string message,
        ILogger? logger = default,
        LogLevel logLevel = LogLevel.Warning)
    {
        AddError($"{WorkflowName}::{action} - {message}");
        logger?.Log(logLevel, ResultMessageFormat, WorkflowName, action, message);
    }

    public void Merge(
        ValidationResult? other,
        string action,
        string message,
        ILogger? logger = default,
        LogLevel logLevel = LogLevel.Information)
    {
        Merge(other);
        logger?.Log(logLevel, ResultMessageFormat, WorkflowName, action, message);
    }
}
