using AIDocumentPipeline.Shared;

namespace AIDocumentPipeline.Invoices;

/// <summary>
/// Defines a model for grouping invoice files by folder.
/// </summary>
public class InvoiceFolder : BaseWorkflowRequest
{
    /// <summary>
    /// Gets or sets the name of the blob container which contains the invoices.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Gets or sets the name of the folder.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of invoice file names in the folder.
    /// </summary>
    public List<string> InvoiceFileNames { get; set; } = new();

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(Container))
        {
            result.AddError($"{nameof(Container)} is required.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            result.AddError($"{nameof(Name)} is required.");
        }

        if (InvoiceFileNames.Count == 0)
        {
            result.AddError($"{nameof(InvoiceFileNames)} are required.");
        }

        return result;
    }
}
