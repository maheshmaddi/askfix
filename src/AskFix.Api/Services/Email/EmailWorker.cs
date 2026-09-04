using System.Threading.Channels;

namespace AskFix.Api.Services.Email;

/// <summary>Bounded in-memory queue; writers never block.</summary>
public class ChannelEmailQueue(ILogger<ChannelEmailQueue> logger) : IEmailQueue
{
    private readonly Channel<EmailJob> _channel = Channel.CreateBounded<EmailJob>(new BoundedChannelOptions(200)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
    });

    public bool TryEnqueue(EmailJob job)
    {
        if (_channel.Writer.TryWrite(job)) return true;
        logger.LogWarning("Email queue full, dropped message to {To}", job.To);
        return false;
    }

    public ChannelReader<EmailJob> Reader => _channel.Reader;
}

/// <summary>Drains the email queue in the background. Retries each message once.</summary>
public class EmailWorker(ChannelEmailQueue queue, EmailSettingsService settings, IEmailSender sender, ILogger<EmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // failures are retried once after a short delay, then dropped (logged) — no dead-letter store
        while (!stoppingToken.IsCancellationRequested)
        {
            EmailJob? job = null;
            try
            {
                job = await queue.Reader.ReadAsync(stoppingToken);
                var s = settings.Load();
                if (!s.IsConfigured)
                {
                    continue; // email disabled/unconfigured: drain silently
                }
                try
                {
                    await sender.SendAsync(job, s, stoppingToken);
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    await sender.SendAsync(job, s, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email to {To} failed permanently", job?.To ?? "?");
            }
        }
    }
}
