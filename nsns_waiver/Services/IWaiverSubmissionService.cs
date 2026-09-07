namespace nsns_waiver.Services;

/// <summary>
/// Defines event lookup and waiver-submission business operations.
/// </summary>
public interface IWaiverSubmissionService
{
    /// <summary>
    /// Resolves an event code against server-side configuration.
    /// </summary>
    WaiverEventInfo? FindEvent(string? eventCode);

    /// <summary>
    /// Validates and atomically saves a complete waiver submission.
    /// </summary>
    Task<SubmitWaiverResult> SubmitAsync(
        SubmitWaiverRequest request,
        CancellationToken cancellationToken = default);
}
