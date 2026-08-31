using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using nsns_waiver.Options;

namespace nsns_waiver.Services;

/// <summary>
/// Compares submitted admin credentials with configured values.
/// </summary>
public sealed class AdminCredentialValidator : IAdminCredentialValidator
{
    private readonly AdminOptions _options;

    /// <summary>
    /// Creates the validator from the Admin configuration section.
    /// </summary>
    public AdminCredentialValidator(IOptions<AdminOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Rejects missing values and compares both credentials in fixed time.
    /// </summary>
    public bool IsValid(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(_options.Username)
            || string.IsNullOrEmpty(_options.Password)
            || username is null
            || password is null)
        {
            return false;
        }

        return FixedTimeEquals(username, _options.Username)
            && FixedTimeEquals(password, _options.Password);
    }

    /// <summary>
    /// Hashes variable-length strings before constant-time byte comparison.
    /// </summary>
    private static bool FixedTimeEquals(string supplied, string configured)
    {
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, configuredHash);
    }
}
