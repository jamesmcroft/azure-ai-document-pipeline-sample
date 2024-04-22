namespace AIDocumentPipeline.Shared;

public class WorkflowActionResult : ValidationResult
{
    /// <summary>
    /// Gets or sets the name of the action associated with the result.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
