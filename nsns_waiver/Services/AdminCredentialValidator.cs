using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using nsns_waiver.Options;

namespace nsns_waiver.Services;

public sealed class AdminCredentialValidator : IAdminCredentialValidator
{
    private readonly AdminOptions _options;

    public AdminCredentialValidator(IOptions<AdminOptions> options)
    {
        _options = options.Value;
    }

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

    private static bool FixedTimeEquals(string supplied, string configured)
    {
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, configuredHash);
    }
}
