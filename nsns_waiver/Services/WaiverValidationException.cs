namespace nsns_waiver.Services;

public sealed class WaiverValidationException : Exception
{
    public WaiverValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("The waiver submission contains invalid data.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
