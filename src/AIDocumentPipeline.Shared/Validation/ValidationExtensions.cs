using System.Globalization;

namespace AIDocumentPipeline.Shared.Validation;

/// <summary>
/// Defines a set of extension methods for validating data.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Checks whether a phrase contains a specified value using a comparison option.
    /// </summary>
    /// <param name="phrase">
    /// The phrase to check.
    /// </param>
    /// <param name="value">
    /// The value to find.
    /// </param>
    /// <param name="culture">
    /// The culture to use for the comparison.
    /// </param>
    /// <param name="compareOption">
    /// The compare option.
    /// </param>
    /// <returns>
    /// True if the phrase contains the value; otherwise, false.
    /// </returns>
    public static bool Contains(this string phrase, string value, CultureInfo culture, CompareOptions compareOption)
    {
        return culture.CompareInfo.IndexOf(phrase, value, compareOption) >= 0;
    }
}
