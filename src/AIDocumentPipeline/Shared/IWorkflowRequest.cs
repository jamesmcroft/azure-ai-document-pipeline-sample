using AIDocumentPipeline.Shared.Observability;

namespace AIDocumentPipeline.Shared;

/// <summary>
/// Defines an interface for the input to a workflow or activity.
/// </summary>
public interface IWorkflowRequest : IObservabilityContext
{
    /// <summary>
    /// Validates the input.
    /// </summary>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the input is valid.</returns>
    ValidationResult Validate();
}
