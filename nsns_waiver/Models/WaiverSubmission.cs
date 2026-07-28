namespace nsns_waiver.Models;

public sealed class WaiverSubmission
{
    public ulong Id { get; set; }
    public required string SubmissionReference { get; init; }
    public required string EventCode { get; init; }
    public required string EventName { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? WechatName { get; init; }
    public required string Email { get; init; }
    public required string NormalizedEmail { get; init; }
    public required string Phone { get; init; }
    public required string NormalizedPhone { get; init; }
    public required string SignatureName { get; init; }
    public bool Agreed { get; init; }
    public DateTime SignedAtUtc { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime CreatedAtUtc { get; set; }
}
