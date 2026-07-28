using nsns_waiver.Models;

namespace nsns_waiver.Repositories;

public interface IEmailOutboxRepository
{
    Task<ulong> InsertAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailOutboxMessage>> GetPendingAsync(
        int limit,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);

    Task MarkSentAsync(
        ulong id,
        DateTime sentAtUtc,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        ulong id,
        string safeErrorSummary,
        DateTime attemptedAtUtc,
        DateTime? nextAttemptAtUtc,
        CancellationToken cancellationToken = default);
}
