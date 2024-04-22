namespace AIDocumentPipeline.Shared;

/// <summary>
/// Defines a result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the result is valid.
    /// </summary>
    /// <remarks>
    /// This property is set to <see langword="true"/> by default.
    /// To set the validation result as invalid, use the <see cref="AddError"/> method.
    /// </remarks>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Gets or sets the result messages.
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// Adds a message to the result.
    /// </summary>
    /// <param name="message">The message to add.</param>
    public void AddMessage(string message)
    {
        Messages.Add(message);
    }

    /// <summary>
    /// Adds an error message to the result and sets the result as invalid.
    /// </summary>
    /// <param name="message">The error message to add.</param>
    public void AddError(string message)
    {
        IsValid = false;
        AddMessage(message);
    }

    /// <summary>
    /// Merges the result with another.
    /// </summary>
    /// <remarks>
    /// The <see cref="IsValid"/> property is updated to <see langword="false"/> if the other result is invalid.
    /// </remarks>
    /// <param name="other">The other result to merge.</param>
    public void Merge(ValidationResult? other)
    {
        if (other == null)
        {
            AddError($"{nameof(other)} is required to merge validation results.");
            return;
        }

        IsValid &= other.IsValid;

        Messages.AddRange(other.Messages);
    }

    /// <summary>
    /// Returns a string combining the messages.
    /// </summary>
    /// <returns>The combined validation messages.</returns>
    public override string ToString()
    {
        return string.Join(", ", Messages);
    }
}
