namespace nsns_waiver.Services;

/// <summary>
/// Reports server-side validation failures grouped by input field.
/// </summary>
public sealed class WaiverValidationException : Exception
{
    /// <summary>
    /// Creates the exception from a field-to-messages validation dictionary.
    /// </summary>
    public WaiverValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("The waiver submission contains invalid data.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
