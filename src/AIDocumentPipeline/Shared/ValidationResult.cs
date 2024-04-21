namespace AIDocumentPipeline.Shared;

/// <summary>
/// Defines a result of a validation operation.
/// </summary>
public class ValidationResult
{
    private readonly List<string> _messages = new();

    /// <summary>
    /// Gets a value indicating whether the result is valid.
    /// </summary>
    /// <remarks>
    /// This property is set to <see langword="true"/> by default.
    /// To set the validation result as invalid, use the <see cref="AddError"/> method.
    /// </remarks>
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Gets or sets the result messages.
    /// </summary>
    public IEnumerable<string> Messages => _messages;

    /// <summary>
    /// Adds a message to the result.
    /// </summary>
    /// <param name="message">The message to add.</param>
    public void Add(string message)
    {
        _messages.Add(message);
    }

    /// <summary>
    /// Adds a range of messages to the result.
    /// </summary>
    /// <param name="messages">The messages to add.</param>
    public void AddRange(IEnumerable<string> messages)
    {
        _messages.AddRange(messages);
    }

    /// <summary>
    /// Adds an error message to the result and sets the result as invalid.
    /// </summary>
    /// <param name="message">The error message to add.</param>
    public void AddError(string message)
    {
        IsValid = false;
        Add(message);
    }

    /// <summary>
    /// Merges the result with another.
    /// </summary>
    /// <remarks>
    /// The <see cref="IsValid"/> property is updated to <see langword="false"/> if the other result is invalid.
    /// </remarks>
    /// <param name="other">The other result to merge.</param>
    /// <param name="errorMessage">The error message to add if the other result is null.</param>
    public void Merge(ValidationResult? other, string? errorMessage = default)
    {
        if (other == null)
        {
            AddError(errorMessage ?? "The validation result to merge is invalid.");
            return;
        }

        IsValid &= other.IsValid;

        _messages.AddRange(other.Messages);
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
