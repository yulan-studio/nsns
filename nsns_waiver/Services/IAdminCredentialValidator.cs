namespace nsns_waiver.Services;

/// <summary>
/// Validates credentials for the protected administrator area.
/// </summary>
public interface IAdminCredentialValidator
{
    /// <summary>
    /// Returns true only when both supplied credentials match configuration.
    /// </summary>
    bool IsValid(string? username, string? password);
}
