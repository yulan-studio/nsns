using nsns_waiver.Models;

namespace nsns_waiver.Repositories;

public interface IWaiverSubmissionRepository
{
    Task<ulong> InsertSubmissionAsync(
        WaiverSubmission submission,
        CancellationToken cancellationToken = default);

    Task<ulong> InsertFamilyMemberAsync(
        ulong submissionId,
        WaiverFamilyMember familyMember,
        CancellationToken cancellationToken = default);

    Task<WaiverSubmission?> GetBySubmissionReferenceAsync(
        string submissionReference,
        CancellationToken cancellationToken = default);

    Task<ulong> CreateSubmissionAsync(
        WaiverSubmission submission,
        IReadOnlyCollection<WaiverFamilyMember> familyMembers,
        IReadOnlyCollection<EmailOutboxMessage> outboxMessages,
        CancellationToken cancellationToken = default);
}
