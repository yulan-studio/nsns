using Microsoft.Extensions.Options;
using nsns_waiver.Options;
using nsns_waiver.Services;

namespace nsns_waiver.Tests;

public sealed class AdminCredentialValidatorTests
{
    [Fact]
    public void IsValid_WithMatchingCredentials_ReturnsTrue()
    {
        var validator = CreateValidator("boss", "strong-password");

        Assert.True(validator.IsValid("boss", "strong-password"));
    }

    [Theory]
    [InlineData("other", "strong-password")]
    [InlineData("boss", "wrong-password")]
    [InlineData(null, "strong-password")]
    [InlineData("boss", null)]
    public void IsValid_WithIncorrectCredentials_ReturnsFalse(
        string? username,
        string? password)
    {
        var validator = CreateValidator("boss", "strong-password");

        Assert.False(validator.IsValid(username, password));
    }

    [Fact]
    public void IsValid_WhenConfigurationIsMissing_ReturnsFalse()
    {
        var validator = CreateValidator(string.Empty, string.Empty);

        Assert.False(validator.IsValid("boss", "strong-password"));
    }

    private static AdminCredentialValidator CreateValidator(
        string username,
        string password) =>
        new(Microsoft.Extensions.Options.Options.Create(new AdminOptions
        {
            Username = username,
            Password = password
        }));
}
