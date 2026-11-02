using nsns_waiver.Models;

namespace nsns_waiver.Repositories;

/// <summary>
/// Supplies paged, sortable waiver data for the protected admin area.
/// </summary>
public interface IAdminSubmissionRepository
{
    /// <summary>
    /// Returns one page from all submissions.
    /// </summary>
    Task<IReadOnlyList<AdminSubmissionListItem>> GetRecentAsync(
        AdminSubmissionSort sort,
        bool descending,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all submissions for pagination.
    /// </summary>
    Task<int> CountRecentAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lists the database-backed columns that administrators may sort by.
/// </summary>
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
