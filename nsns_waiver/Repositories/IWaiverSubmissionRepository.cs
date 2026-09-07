using nsns_waiver.Models;

namespace nsns_waiver.Repositories;

/// <summary>
/// Defines persistence operations for waivers and their related records.
/// </summary>
public interface IWaiverSubmissionRepository
{
    /// <summary>
    /// Inserts only the main waiver submission record.
    /// </summary>
    Task<ulong> InsertSubmissionAsync(
        WaiverSubmission submission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts one family member for an existing submission.
    /// </summary>
    Task<ulong> InsertFamilyMemberAsync(
        ulong submissionId,
        WaiverFamilyMember familyMember,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a waiver by its public UUID reference, or returns null.
    /// </summary>
    Task<WaiverSubmission?> GetBySubmissionReferenceAsync(
        string submissionReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically inserts a submission, family members, and queued emails.
    /// </summary>
    Task<ulong> CreateSubmissionAsync(
        WaiverSubmission submission,
        IReadOnlyCollection<WaiverFamilyMember> familyMembers,
        IReadOnlyCollection<EmailOutboxMessage> outboxMessages,
        CancellationToken cancellationToken = default);
}
