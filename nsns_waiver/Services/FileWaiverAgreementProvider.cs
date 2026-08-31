namespace nsns_waiver.Services;

/// <summary>
/// Loads the waiver agreement from Content/waiver-agreement.html.
/// </summary>
public sealed class FileWaiverAgreementProvider : IWaiverAgreementProvider
{
    internal const string PlaceholderMarker = "REPLACE_WITH_APPROVED_WAIVER";
    private readonly string _agreementPath;

    /// <summary>
    /// Resolves the agreement's absolute path from the application content root.
    /// </summary>
    public FileWaiverAgreementProvider(IWebHostEnvironment environment)
    {
        _agreementPath = Path.Combine(
            environment.ContentRootPath,
            "Content",
            "waiver-agreement.html");
    }

    /// <summary>
    /// Reads the agreement and disables submissions when it is missing or a placeholder.
    /// </summary>
    public async Task<WaiverAgreementContent> GetAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_agreementPath))
        {
            return new WaiverAgreementContent(
                "<p>The waiver agreement is currently unavailable.</p>",
                false);
        }

        var html = await File.ReadAllTextAsync(_agreementPath, cancellationToken);
        var isApproved = !string.IsNullOrWhiteSpace(html)
            && !html.Contains(PlaceholderMarker, StringComparison.Ordinal);
        return new WaiverAgreementContent(html, isApproved);
    }
}
