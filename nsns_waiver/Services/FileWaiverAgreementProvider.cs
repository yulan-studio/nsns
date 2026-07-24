namespace nsns_waiver.Services;

public sealed class FileWaiverAgreementProvider : IWaiverAgreementProvider
{
    internal const string PlaceholderMarker = "REPLACE_WITH_APPROVED_WAIVER";
    private readonly string _agreementPath;

    public FileWaiverAgreementProvider(IWebHostEnvironment environment)
    {
        _agreementPath = Path.Combine(
            environment.ContentRootPath,
            "Content",
            "waiver-agreement.html");
    }

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
