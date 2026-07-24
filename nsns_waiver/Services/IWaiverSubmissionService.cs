namespace nsns_waiver.Services;

public interface IWaiverSubmissionService
{
    Task<SubmitWaiverResult> SubmitAsync(
        SubmitWaiverRequest request,
        CancellationToken cancellationToken = default);
}
