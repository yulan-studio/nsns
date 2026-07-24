namespace nsns_waiver.Services;

public interface IWaiverSubmissionService
{
    WaiverEventInfo? FindEvent(string? eventCode);

    Task<SubmitWaiverResult> SubmitAsync(
        SubmitWaiverRequest request,
        CancellationToken cancellationToken = default);
}
