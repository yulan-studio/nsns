namespace nsns_waiver.Services;

/// <summary>
/// Returns the safe confirmation details produced after a successful submission.
/// </summary>
public sealed record SubmitWaiverResult(
    string SubmissionReference,
    string EventName,
    DateTime SignedAtUtc);
