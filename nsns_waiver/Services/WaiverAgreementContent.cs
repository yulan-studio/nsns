namespace nsns_waiver.Services;

/// <summary>
/// Contains the agreement HTML and whether it is approved for submissions.
/// </summary>
public sealed record WaiverAgreementContent(string Html, bool IsApproved);
