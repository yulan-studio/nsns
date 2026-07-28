namespace nsns_waiver.Models;

public sealed class AdminSubmissionListItem
{
    public required string EventName { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? WechatName { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string SignatureName { get; init; }
    public DateTime SignedAtUtc { get; init; }
    public string? FamilyMembers { get; init; }
}
