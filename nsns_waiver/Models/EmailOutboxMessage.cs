namespace nsns_waiver.Models;

public sealed class EmailOutboxMessage
{
    public ulong Id { get; set; }
    public ulong SubmissionId { get; set; }
    public required string MessageType { get; init; }
    public required string RecipientEmail { get; init; }
    public required string Subject { get; init; }
    public required string BodyHtml { get; init; }
    public string Status { get; set; } = "Pending";
    public uint AttemptCount { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
