using nsns_waiver.Models;

namespace nsns_waiver.Services;

/// <summary>
/// Abstracts delivery of one queued email message.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends the supplied outbox message through the configured transport.
    /// </summary>
    Task SendAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken = default);
}
