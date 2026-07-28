using Dapper;
using nsns_waiver.Data;
using nsns_waiver.Models;

namespace nsns_waiver.Repositories;

public sealed class EmailOutboxRepository : IEmailOutboxRepository
{
    internal const string InsertSql = """
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

    private const string GetPendingSql = """
        SELECT
            id AS Id,
            submission_id AS SubmissionId,
            message_type AS MessageType,
            recipient_email AS RecipientEmail,
            subject AS Subject,
            body_html AS BodyHtml,
            status AS Status,
            attempt_count AS AttemptCount,
            next_attempt_at_utc AS NextAttemptAtUtc,
            last_attempt_at_utc AS LastAttemptAtUtc,
            sent_at_utc AS SentAtUtc,
            last_error AS LastError,
            created_at_utc AS CreatedAtUtc
        FROM email_outbox
        WHERE status = 'Pending'
           OR (status = 'Failed' AND next_attempt_at_utc <= @AsOfUtc)
        ORDER BY created_at_utc, id
        LIMIT @Limit;
        """;

    private const string MarkSentSql = """
        UPDATE email_outbox
        SET status = 'Sent',
            sent_at_utc = @SentAtUtc,
            last_attempt_at_utc = @SentAtUtc,
            attempt_count = attempt_count + 1,
            last_error = NULL
        WHERE id = @Id;
        """;

    private const string MarkFailedSql = """
        UPDATE email_outbox
        SET status = CASE
                WHEN @NextAttemptAtUtc IS NULL THEN 'Abandoned'
                ELSE 'Failed'
            END,
            last_attempt_at_utc = @AttemptedAtUtc,
            next_attempt_at_utc = @NextAttemptAtUtc,
            attempt_count = attempt_count + 1,
            last_error = @SafeErrorSummary
        WHERE id = @Id;
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public EmailOutboxRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ulong> InsertAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            InsertSql,
            message,
            cancellationToken: cancellationToken);
        var id = await connection.ExecuteScalarAsync<ulong>(command);
        message.Id = id;
        return id;
    }

    public async Task<IReadOnlyList<EmailOutboxMessage>> GetPendingAsync(
        int limit,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be positive.");
        }

        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            GetPendingSql,
            new { Limit = limit, AsOfUtc = asOfUtc },
            cancellationToken: cancellationToken);
        var messages = await connection.QueryAsync<EmailOutboxMessage>(command);
        return messages.AsList();
    }

    public async Task MarkSentAsync(
        ulong id,
        DateTime sentAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            MarkSentSql,
            new { Id = id, SentAtUtc = sentAtUtc },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task MarkFailedAsync(
        ulong id,
        string safeErrorSummary,
        DateTime attemptedAtUtc,
        DateTime? nextAttemptAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            MarkFailedSql,
            new
            {
                Id = id,
                SafeErrorSummary = safeErrorSummary,
                AttemptedAtUtc = attemptedAtUtc,
                NextAttemptAtUtc = nextAttemptAtUtc
            },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
