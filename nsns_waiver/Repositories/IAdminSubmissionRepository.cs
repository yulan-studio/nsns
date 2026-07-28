using nsns_waiver.Models;

namespace nsns_waiver.Repositories;

public interface IAdminSubmissionRepository
{
    Task<IReadOnlyList<AdminSubmissionListItem>> GetRecentAsync(
        AdminSubmissionSort sort,
        bool descending,
        int limit,
        CancellationToken cancellationToken = default);
}

public enum AdminSubmissionSort
{
    SignedAt,
    EventName,
    FirstName,
    LastName,
    Email
}
