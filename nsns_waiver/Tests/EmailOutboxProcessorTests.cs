using System.Net.Mail;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using nsns_waiver.Models;
using nsns_waiver.Options;
using nsns_waiver.Repositories;
using nsns_waiver.Services;

namespace nsns_waiver.Tests;

public sealed class EmailOutboxProcessorTests
{
    [Fact]
    public async Task ProcessBatchAsync_SendsAndMarksMessageSent()
    {
        var message = CreateMessage();
        var repository = new RecordingRepository(message);
        var sender = new RecordingSender();
        var processor = CreateProcessor(repository, sender);

        var processed = await processor.ProcessBatchAsync();

        Assert.Equal(1, processed);
        Assert.Same(message, sender.SentMessage);
        Assert.Equal(message.Id, repository.SentId);
        Assert.Null(repository.FailedId);
    }

    [Fact]
    public async Task ProcessBatchAsync_AbandonsMessageAfterMaximumAttempts()
    {
        var message = CreateMessage();
        message.AttemptCount = 4;
        var repository = new RecordingRepository(message);
        var sender = new RecordingSender(new SmtpException());
        var processor = CreateProcessor(repository, sender);

        var processed = await processor.ProcessBatchAsync();

        Assert.Equal(1, processed);
        Assert.Equal(message.Id, repository.FailedId);
        Assert.Null(repository.NextAttemptAtUtc);
        Assert.Null(repository.SentId);
        Assert.DoesNotContain(
            message.RecipientEmail,
            repository.SafeErrorSummary,
            StringComparison.OrdinalIgnoreCase);
    }

    private static EmailOutboxProcessor CreateProcessor(
        IEmailOutboxRepository repository,
        IEmailSender sender)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new EmailOptions
        {
            BatchSize = 20,
            MaximumAttempts = 5
        });

        return new EmailOutboxProcessor(
            repository,
            sender,
            options,
            TimeProvider.System,
            NullLogger<EmailOutboxProcessor>.Instance);
    }

    private static EmailOutboxMessage CreateMessage() =>
        new()
        {
            Id = 42,
            SubmissionId = 7,
            MessageType = "CustomerConfirmation",
            RecipientEmail = "customer@example.com",
            Subject = "Test",
            BodyHtml = "<p>Test</p>"
        };

    private sealed class RecordingSender(Exception? exception = null) : IEmailSender
    {
        public EmailOutboxMessage? SentMessage { get; private set; }

        public Task SendAsync(
            EmailOutboxMessage message,
            CancellationToken cancellationToken = default)
        {
            if (exception is not null)
            {
                throw exception;
            }

            SentMessage = message;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingRepository(EmailOutboxMessage message)
        : IEmailOutboxRepository
    {
        public ulong? SentId { get; private set; }
        public ulong? FailedId { get; private set; }
        public DateTime? NextAttemptAtUtc { get; private set; }
        public string SafeErrorSummary { get; private set; } = string.Empty;

        public Task<IReadOnlyList<EmailOutboxMessage>> GetPendingAsync(
            int limit,
            DateTime asOfUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmailOutboxMessage>>([message]);

        public Task MarkSentAsync(
            ulong id,
            DateTime sentAtUtc,
            CancellationToken cancellationToken = default)
        {
            SentId = id;
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(
            ulong id,
            string safeErrorSummary,
            DateTime attemptedAtUtc,
            DateTime? nextAttemptAtUtc,
            CancellationToken cancellationToken = default)
        {
            FailedId = id;
            SafeErrorSummary = safeErrorSummary;
            NextAttemptAtUtc = nextAttemptAtUtc;
            return Task.CompletedTask;
        }

        public Task<ulong> InsertAsync(
            EmailOutboxMessage outboxMessage,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
