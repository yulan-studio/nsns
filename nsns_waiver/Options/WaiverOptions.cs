namespace nsns_waiver.Options;

public sealed class WaiverOptions
{
    public const string SectionName = "Waiver";

    public string BusinessOwnerEmail { get; set; } = string.Empty;
    public Dictionary<string, string> Events { get; set; } = [];
}
