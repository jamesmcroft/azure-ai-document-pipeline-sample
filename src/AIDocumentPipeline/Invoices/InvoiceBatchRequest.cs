using AIDocumentPipeline.Shared;

namespace AIDocumentPipeline.Invoices;

/// <summary>
/// Defines a model for processing a batch of invoices from a blob container.
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
