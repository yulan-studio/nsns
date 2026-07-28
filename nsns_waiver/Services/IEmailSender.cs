using nsns_waiver.Models;

namespace nsns_waiver.Services;

public interface IEmailSender
{
    Task SendAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken = default);
}
