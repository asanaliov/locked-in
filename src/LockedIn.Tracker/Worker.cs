using LockedIn.Tracker.Windows;

namespace LockedIn.Tracker;

public sealed class Worker(IActiveWindowProvider windows, ILogger<Worker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            var window = windows.GetActiveWindow();
            logger.LogInformation("Active: {App} | {Title}", window?.AppName, window?.Title);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
