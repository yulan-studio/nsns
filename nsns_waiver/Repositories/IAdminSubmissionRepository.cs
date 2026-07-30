using nsns_waiver.Models;

namespace nsns_waiver.Repositories;

/// <summary>
/// Supplies paged, sortable waiver data for the protected admin area.
/// </summary>
public interface IAdminSubmissionRepository
{
    /// <summary>
    /// Returns one page from the 200 most recent submissions.
    /// </summary>
    Task<IReadOnlyList<AdminSubmissionListItem>> GetRecentAsync(
        AdminSubmissionSort sort,
        bool descending,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts recent submissions up to the requested display maximum.
    /// </summary>
    Task<int> CountRecentAsync(
        int maximum,
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
