using Microsoft.Extensions.Options;
using nsns_waiver.Options;
using nsns_waiver.Repositories;

namespace nsns_waiver.Services;

public sealed class EmailOutboxProcessor
{
    private readonly IEmailOutboxRepository _repository;
    private readonly IEmailSender _sender;
    private readonly EmailOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EmailOutboxProcessor> _logger;

    public EmailOutboxProcessor(
        IEmailOutboxRepository repository,
        IEmailSender sender,
        IOptions<EmailOptions> options,
        TimeProvider timeProvider,
        ILogger<EmailOutboxProcessor> logger)
    {
        _repository = repository;
        _sender = sender;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(
        CancellationToken cancellationToken = default)
    {
        var batchSize = Math.Clamp(_options.BatchSize, 1, 100);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var messages = await _repository.GetPendingAsync(
            batchSize,
            nowUtc,
            cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await _sender.SendAsync(message, cancellationToken);
                await _repository.MarkSentAsync(
                    message.Id,
                    _timeProvider.GetUtcNow().UtcDateTime,
                    cancellationToken);
                _logger.LogInformation(
                    "Email outbox message {OutboxMessageId} was sent.",
                    message.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var attemptedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
                var maximumAttempts = Math.Clamp(_options.MaximumAttempts, 1, 20);
                var nextAttemptNumber = message.AttemptCount + 1;
                DateTime? nextAttemptAtUtc = nextAttemptNumber >= maximumAttempts
                    ? null
                    : attemptedAtUtc.AddMinutes(
                        Math.Pow(2, Math.Min(nextAttemptNumber - 1, 6)));

                await _repository.MarkFailedAsync(
                    message.Id,
                    $"{exception.GetType().Name}: Email delivery failed.",
                    attemptedAtUtc,
                    nextAttemptAtUtc,
                    cancellationToken);
                _logger.LogWarning(
                    "Email outbox message {OutboxMessageId} failed on attempt {AttemptNumber}.",
                    message.Id,
                    nextAttemptNumber);
            }
        }

        return messages.Count;
    }
}
