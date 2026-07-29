using Dapper;
using nsns_waiver.Data;
using nsns_waiver.Models;

namespace nsns_waiver.Repositories;

public sealed class AdminSubmissionRepository : IAdminSubmissionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AdminSubmissionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<AdminSubmissionListItem>> GetRecentAsync(
        AdminSubmissionSort sort,
        bool descending,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var orderColumn = sort switch
        {
            AdminSubmissionSort.EventName => "s.event_name",
            AdminSubmissionSort.FirstName => "s.first_name",
            AdminSubmissionSort.LastName => "s.last_name",
            AdminSubmissionSort.Email => "s.email",
            AdminSubmissionSort.Phone => "s.phone",
            AdminSubmissionSort.SignatureName => "s.signature_name",
            _ => "s.signed_at_utc"
        };
        var direction = descending ? "DESC" : "ASC";
        var sql = $$"""
            SELECT
                s.event_name AS EventName,
                s.first_name AS FirstName,
                s.last_name AS LastName,
                s.wechat_name AS WechatName,
                s.email AS Email,
                s.phone AS Phone,
                s.signature_name AS SignatureName,
                s.media_release_agreed AS MediaReleaseAgreed,
                s.signed_at_utc AS SignedAtUtc,
                (
                    SELECT GROUP_CONCAT(
                        CONCAT(
                            f.first_name, ' ', f.last_name,
                            CASE
                                WHEN f.relationship IS NULL OR f.relationship = '' THEN ''
                                ELSE CONCAT(' (', f.relationship, ')')
                            END)
                        ORDER BY f.id SEPARATOR ', ')
                    FROM waiver_family_members f
                    WHERE f.submission_id = s.id
                ) AS FamilyMembers
            FROM (
                SELECT *
                FROM waiver_submissions
                ORDER BY signed_at_utc DESC, id DESC
                LIMIT 200
            ) s
            ORDER BY {{orderColumn}} {{direction}}, s.id DESC
            LIMIT @Limit OFFSET @Offset;
            """;

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                Limit = Math.Clamp(limit, 1, 200),
                Offset = Math.Clamp(offset, 0, 199)
            },
            cancellationToken: cancellationToken);
        var submissions =
            await connection.QueryAsync<AdminSubmissionListItem>(command);
        return submissions.AsList();
    }

    public async Task<int> CountRecentAsync(
        int maximum,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT LEAST(COUNT(*), @Maximum)
            FROM waiver_submissions;
            """;

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { Maximum = Math.Clamp(maximum, 1, 200) },
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
    }
}
