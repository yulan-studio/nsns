namespace nsns_waiver.Services;

public sealed class SubmitWaiverRequest
{
    public required string EventCode { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? WechatName { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string SignatureName { get; init; }
    public bool Agreed { get; init; }
    public IReadOnlyCollection<SubmitWaiverFamilyMember> FamilyMembers { get; init; } = [];
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

public sealed class SubmitWaiverFamilyMember
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? Relationship { get; init; }
}
