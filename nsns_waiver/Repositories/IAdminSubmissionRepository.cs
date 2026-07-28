using nsns_waiver.Models;

namespace nsns_waiver.Repositories;

public interface IAdminSubmissionRepository
{
    Task<IReadOnlyList<AdminSubmissionListItem>> GetRecentAsync(
        AdminSubmissionSort sort,
        bool descending,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);

    Task<int> CountRecentAsync(
        int maximum,
        CancellationToken cancellationToken = default);
}

public enum AdminSubmissionSort
{
    SignedAt,
    EventName,
    FirstName,
    LastName,
    Email,
    Phone,
    SignatureName
}
