namespace nsns_waiver.Services;

public interface IAdminCredentialValidator
{
    bool IsValid(string? username, string? password);
}
