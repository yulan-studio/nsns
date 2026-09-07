using nsns_waiver.Models;

namespace nsns_waiver.Repositories;

/// <summary>
/// Defines storage operations for the reliable email outbox.
/// </summary>
public interface IEmailOutboxRepository
{
    /// <summary>
    /// Adds one message to the email outbox.
    /// </summary>
    Task<ulong> InsertAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns pending or retryable messages in delivery order.
    /// </summary>
    Task<IReadOnlyList<EmailOutboxMessage>> GetPendingAsync(
        int limit,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records successful delivery and increments the attempt count.
    /// </summary>
    Task MarkSentAsync(
        ulong id,
        DateTime sentAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed attempt and either schedules a retry or abandons the message.
    /// </summary>
    Task MarkFailedAsync(
        ulong id,
        string safeErrorSummary,
        DateTime attemptedAtUtc,
        DateTime? nextAttemptAtUtc,
        CancellationToken cancellationToken = default);
}
