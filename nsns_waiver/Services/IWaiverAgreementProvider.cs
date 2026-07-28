namespace nsns_waiver.Services;

public interface IWaiverAgreementProvider
{
    Task<WaiverAgreementContent> GetAsync(
        CancellationToken cancellationToken = default);
}
