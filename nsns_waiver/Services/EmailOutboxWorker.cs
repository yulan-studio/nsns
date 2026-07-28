using Microsoft.Extensions.Options;
using nsns_waiver.Options;

namespace nsns_waiver.Services;

public sealed class EmailOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailOptions _options;
    private readonly ILogger<EmailOutboxWorker> _logger;

    public EmailOutboxWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailOptions> options,
        ILogger<EmailOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Email delivery is disabled. Queued messages will remain pending.");
            return;
        }

        _logger.LogInformation("Email outbox delivery worker started.");
        var pollInterval = TimeSpan.FromSeconds(
            Math.Clamp(_options.PollIntervalSeconds, 1, 300));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor =
                    scope.ServiceProvider.GetRequiredService<EmailOutboxProcessor>();
                var processed = await processor.ProcessBatchAsync(stoppingToken);

                if (processed == 0)
                {
                    await Task.Delay(pollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Email outbox processing failed. Processing will retry.");
                await Task.Delay(pollInterval, stoppingToken);
            }
        }
    }
}
