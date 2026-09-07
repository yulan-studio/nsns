using Dapper;
using MySqlConnector;
using nsns_waiver.Data;
using nsns_waiver.Models;

namespace nsns_waiver.Repositories;

/// <summary>
/// Uses Dapper and parameterized SQL to persist complete waiver submissions.
/// </summary>
public sealed class WaiverSubmissionRepository : IWaiverSubmissionRepository
{
    internal const string InsertSubmissionSql = """
        INSERT INTO waiver_submissions (
            submission_reference, event_code, event_name, first_name, last_name,
            wechat_name, email, normalized_email, phone, normalized_phone,
            signature_name, agreed, media_release_agreed, signed_at_utc,
            ip_address, user_agent)
        VALUES (
            @SubmissionReference, @EventCode, @EventName, @FirstName, @LastName,
            @WechatName, @Email, @NormalizedEmail, @Phone, @NormalizedPhone,
            @SignatureName, @Agreed, @MediaReleaseAgreed, @SignedAtUtc,
            @IpAddress, @UserAgent);
        SELECT LAST_INSERT_ID();
        """;

    internal const string InsertFamilyMemberSql = """
        INSERT INTO waiver_family_members (
            submission_id, first_name, last_name, relationship)
        VALUES (
            @SubmissionId, @FirstName, @LastName, @Relationship);
        SELECT LAST_INSERT_ID();
        """;

    internal const string InsertOutboxSql = """
        INSERT INTO email_outbox (
            submission_id, message_type, recipient_email, subject, body_html,
            status, attempt_count, next_attempt_at_utc, last_attempt_at_utc,
            sent_at_utc, last_error)
        VALUES (
            @SubmissionId, @MessageType, @RecipientEmail, @Subject, @BodyHtml,
            @Status, @AttemptCount, @NextAttemptAtUtc, @LastAttemptAtUtc,
            @SentAtUtc, @LastError);
        SELECT LAST_INSERT_ID();
        """;

    private const string GetByReferenceSql = """
        SELECT
            id AS Id,
            submission_reference AS SubmissionReference,
            event_code AS EventCode,
            event_name AS EventName,
            first_name AS FirstName,
            last_name AS LastName,
            wechat_name AS WechatName,
            email AS Email,
            normalized_email AS NormalizedEmail,
            phone AS Phone,
            normalized_phone AS NormalizedPhone,
            signature_name AS SignatureName,
            agreed AS Agreed,
            media_release_agreed AS MediaReleaseAgreed,
            signed_at_utc AS SignedAtUtc,
            ip_address AS IpAddress,
            user_agent AS UserAgent,
            created_at_utc AS CreatedAtUtc
        FROM waiver_submissions
        WHERE submission_reference = @SubmissionReference
        LIMIT 1;
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates the repository with the shared database connection factory.
    /// </summary>
    public WaiverSubmissionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Inserts a standalone submission and updates its generated database ID.
    /// </summary>
    public async Task<ulong> InsertSubmissionAsync(
        WaiverSubmission submission,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var id = await InsertSubmissionCoreAsync(
            connection, null, submission, cancellationToken);
        submission.Id = id;
        return id;
    }

    /// <summary>
    /// Inserts a standalone family member and updates its generated IDs.
    /// </summary>
    public async Task<ulong> InsertFamilyMemberAsync(
        ulong submissionId,
        WaiverFamilyMember familyMember,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var id = await InsertFamilyMemberCoreAsync(
            connection, null, submissionId, familyMember, cancellationToken);
        familyMember.Id = id;
        familyMember.SubmissionId = submissionId;
        return id;
    }

    /// <summary>
    /// Loads a submission by the UUID reference shown to the customer.
    /// </summary>
    public async Task<WaiverSubmission?> GetBySubmissionReferenceAsync(
        string submissionReference,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            GetByReferenceSql,
            new { SubmissionReference = submissionReference },
            cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WaiverSubmission>(command);
    }

    /// <summary>
    /// Saves the waiver, family members, and outbox messages in one transaction.
    /// </summary>
    public async Task<ulong> CreateSubmissionAsync(
        WaiverSubmission submission,
        IReadOnlyCollection<WaiverFamilyMember> familyMembers,
        IReadOnlyCollection<EmailOutboxMessage> outboxMessages,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Nothing related to the waiver becomes visible unless every insert succeeds.
            var submissionId = await InsertSubmissionCoreAsync(
                connection, transaction, submission, cancellationToken);

            foreach (var familyMember in familyMembers)
            {
                familyMember.SubmissionId = submissionId;
                familyMember.Id = await InsertFamilyMemberCoreAsync(
                    connection,
                    transaction,
                    submissionId,
                    familyMember,
                    cancellationToken);
            }

            foreach (var outboxMessage in outboxMessages)
            {
                outboxMessage.SubmissionId = submissionId;
                outboxMessage.Id = await InsertOutboxCoreAsync(
                    connection,
                    transaction,
                    submissionId,
                    outboxMessage,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            submission.Id = submissionId;
            return submissionId;
        }
        catch
        {
            // Ignore request cancellation while rolling back; database consistency wins.
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Executes the submission insert with an optional transaction.
    /// </summary>
    private static Task<ulong> InsertSubmissionCoreAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        WaiverSubmission submission,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            InsertSubmissionSql,
            submission,
            transaction,
            cancellationToken: cancellationToken);
        return connection.ExecuteScalarAsync<ulong>(command);
    }

    /// <summary>
    /// Executes the family-member insert with an optional transaction.
    /// </summary>
    private static Task<ulong> InsertFamilyMemberCoreAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        ulong submissionId,
        WaiverFamilyMember familyMember,
        CancellationToken cancellationToken)
    {
        var parameters = new
        {
            SubmissionId = submissionId,
            familyMember.FirstName,
            familyMember.LastName,
            familyMember.Relationship
        };
        var command = new CommandDefinition(
            InsertFamilyMemberSql,
            parameters,
            transaction,
            cancellationToken: cancellationToken);
        return connection.ExecuteScalarAsync<ulong>(command);
    }

    /// <summary>
    /// Executes the outbox insert inside the submission transaction.
    /// </summary>
    private static Task<ulong> InsertOutboxCoreAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        ulong submissionId,
        EmailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var parameters = new
        {
            SubmissionId = submissionId,
            message.MessageType,
            message.RecipientEmail,
            message.Subject,
            message.BodyHtml,
            message.Status,
            message.AttemptCount,
            message.NextAttemptAtUtc,
            message.LastAttemptAtUtc,
            message.SentAtUtc,
            message.LastError
        };
        var command = new CommandDefinition(
            InsertOutboxSql,
            parameters,
            transaction,
            cancellationToken: cancellationToken);
        return connection.ExecuteScalarAsync<ulong>(command);
    }
}
