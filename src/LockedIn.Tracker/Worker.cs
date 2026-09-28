using LockedIn.Tracker.Windows;
using Microsoft.Extensions.Options;

namespace LockedIn.Tracker;

public sealed class Worker(
    IActiveWindowProvider windows,
    IIdleDetector idleDetector,
    IOptions<TrackerOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var wasIdle = false;

        using var timer = new PeriodicTimer(settings.PollInterval);
        do
        {
            var isIdle = idleDetector.GetIdleTime() > settings.IdleThreshold;
            if (isIdle != wasIdle)
                logger.LogInformation(isIdle ? "Went idle" : "Input returned");
            wasIdle = isIdle;

            if (isIdle)
                continue;

            var window = windows.GetActiveWindow();
            logger.LogInformation("Active: {App} | {Title}", window?.AppName, window?.Title);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
