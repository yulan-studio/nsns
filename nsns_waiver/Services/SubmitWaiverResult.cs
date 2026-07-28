namespace nsns_waiver.Services;

public sealed record SubmitWaiverResult(
    string SubmissionReference,
    string EventName,
    DateTime SignedAtUtc);
