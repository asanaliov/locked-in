using LockedIn.Data;
using LockedIn.Tracker.Sessions;
using Microsoft.Extensions.Options;

namespace LockedIn.Tracker;

public sealed class Worker(
    SessionTracker tracker,
    IServiceProvider services,
    IOptions<TrackerOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await services.InitializeLockedInDatabaseAsync(stoppingToken);

        using var timer = new PeriodicTimer(options.Value.PollInterval);
        do
        {
            try
            {
                await tracker.TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Tracking tick failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await tracker.FlushAsync(CancellationToken.None);
    }
}
