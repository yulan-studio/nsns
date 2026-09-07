namespace nsns_waiver.Services;

/// <summary>
/// Provides the HTML waiver agreement displayed on the public form.
/// </summary>
public interface IWaiverAgreementProvider
{
    /// <summary>
    /// Loads the current agreement and indicates whether submissions may use it.
    /// </summary>
    Task<WaiverAgreementContent> GetAsync(
        CancellationToken cancellationToken = default);
}
