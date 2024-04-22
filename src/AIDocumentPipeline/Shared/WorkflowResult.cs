using Microsoft.Extensions.Logging;

namespace AIDocumentPipeline.Shared;

/// <summary>
/// Defines a model that represents the result of a workflow or activity.
/// </summary>
public class WorkflowResult : ValidationResult
{
    private const string ResultMessageFormat = "{Name}::{Action} - {Message}";

    /// <summary>
    /// Gets or sets a unique name to represent the result.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a collection of results associated with actions performed during the workflow.
    /// </summary>
    public List<WorkflowResult> ActivityResults { get; set; } = new();

    /// <summary>
    /// Adds a message to the result and logs the message using the specified logger.
    /// </summary>
    /// <param name="action">The action that was performed.</param>
    /// <param name="message">The message to add to the results.</param>
    /// <param name="logger">The logger to use for logging the result.</param>
    /// <param name="logLevel">The log level to use when logging the result.</param>
    public void AddMessage(
        string action,
        string message,
        ILogger? logger = default,
        LogLevel logLevel = LogLevel.Information)
    {
        AddMessage($"{Name}::{action} - {message}");
        logger?.Log(logLevel, ResultMessageFormat, Name, action, message);
    }

    /// <summary>
    /// Adds an error message to the result and logs the message using the specified logger.
    /// </summary>
    /// <param name="action">The action that was performed.</param>
    /// <param name="message">The error message to add to the results.</param>
    /// <param name="logger">The logger to use for logging the result.</param>
    /// <param name="logLevel">The log level to use when logging the result.</param>
    public void AddError(
        string action,
        string message,
        ILogger? logger = default,
        LogLevel logLevel = LogLevel.Warning)
    {
        AddError($"{Name}::{action} - {message}");
        logger?.Log(logLevel, ResultMessageFormat, Name, action, message);
    }

    /// <summary>
    /// Adds a result to the collection of activity results and logs the message using the specified logger.
    /// </summary>
    /// <param name="action">The action that was performed.</param>
    /// <param name="message">The message to add to the results.</param>
    /// <param name="result">The result of the action.</param>
    /// <param name="logger">The logger to use for logging the result.</param>
    /// <param name="logLevel">The log level to use when logging the result.</param>
    public void AddActivityResult(
        string action,
        string message,
        WorkflowResult result,
        ILogger? logger = default,
        LogLevel logLevel = LogLevel.Information)
    {
        ActivityResults.Add(result);
        logger?.Log(logLevel, ResultMessageFormat, Name, action, message);
    }

    /// <summary>
    /// Returns the name of the result.
    /// </summary>
    /// <returns>The name of the result.</returns>
    public override string ToString()
    {
        return Name;
    }
}
