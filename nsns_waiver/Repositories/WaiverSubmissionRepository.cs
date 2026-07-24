using Dapper;
using MySqlConnector;
using nsns_waiver.Data;
using nsns_waiver.Models;

namespace nsns_waiver.Repositories;

public sealed class WaiverSubmissionRepository : IWaiverSubmissionRepository
{
    internal const string InsertSubmissionSql = """
        INSERT INTO waiver_submissions (
            submission_reference, event_code, event_name, first_name, last_name,
            wechat_name, email, normalized_email, phone, normalized_phone,
            signature_name, agreed, signed_at_utc, ip_address, user_agent)
        VALUES (
            @SubmissionReference, @EventCode, @EventName, @FirstName, @LastName,
            @WechatName, @Email, @NormalizedEmail, @Phone, @NormalizedPhone,
            @SignatureName, @Agreed, @SignedAtUtc, @IpAddress, @UserAgent);
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
            signed_at_utc AS SignedAtUtc,
            ip_address AS IpAddress,
            user_agent AS UserAgent,
            created_at_utc AS CreatedAtUtc
        FROM waiver_submissions
        WHERE submission_reference = @SubmissionReference
        LIMIT 1;
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public WaiverSubmissionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

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
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

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
