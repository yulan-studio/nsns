namespace nsns_waiver.Options;

/// <summary>
/// Maps the configured administrator username and password.
/// </summary>
public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
