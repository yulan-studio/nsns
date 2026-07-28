namespace nsns_waiver.Models;

public sealed class WaiverFamilyMember
{
    public ulong Id { get; set; }
    public ulong SubmissionId { get; set; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? Relationship { get; init; }
    public DateTime CreatedAtUtc { get; set; }
}
