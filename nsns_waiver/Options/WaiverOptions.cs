namespace nsns_waiver.Options;

/// <summary>
/// Maps the Waiver configuration section, including allowed events and owner email.
/// </summary>
public sealed class WaiverOptions
{
    public const string SectionName = "Waiver";

    public string BusinessOwnerEmail { get; set; } = string.Empty;
    public Dictionary<string, string> Events { get; set; } = [];
}
