using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LockedIn.Core.Updates;

/// <summary>
/// Checks GitHub for a newer release a couple of minutes after start and then once a day: one small request,
/// with nothing about the user sent. Installing downloads the new installer, runs it silently and exits so it
/// can replace the app; the installer starts the new version when it's done.
/// </summary>
public sealed class UpdateService(IOptionsMonitor<UpdateOptions> options, ILogger<UpdateService> logger) : BackgroundService
{
    private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromDays(1);
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(15);
    private static readonly string DownloadFolder = Path.Combine(Path.GetTempPath(), "LockedIn-update");

    public static Version CurrentVersion { get; } = typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0);

    /// <summary>The newest release found so far, if it is newer than this one.</summary>
    public AvailableUpdate? Available { get; private set; }

    /// <summary>Raised on a background thread the first time a given new version is found.</summary>
    public event Action<AvailableUpdate>? UpdateFound;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(FirstCheckDelay, stoppingToken);
        DeleteDownloadedInstallers();

        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            if (!options.CurrentValue.CheckAutomatically)
                continue;

            try
            {
                await CheckAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                // Offline or GitHub unavailable: try again tomorrow.
                logger.LogDebug(ex, "Update check failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<AvailableUpdate?> CheckAsync(CancellationToken cancellationToken)
    {
        using var client = CreateClient();
        var json = await client.GetStringAsync(ReleaseFeed.LatestReleaseUrl, cancellationToken);
        var update = ReleaseFeed.Parse(json, CurrentVersion);

        if (update is not null && update.Version != Available?.Version)
        {
            Available = update;
            UpdateFound?.Invoke(update);
        }
        return update;
    }

    /// <summary>Windows only. Downloads and starts the installer; the caller then exits the app. Throws if the download fails.</summary>
    public async Task InstallAsync(AvailableUpdate update)
    {
        Directory.CreateDirectory(DownloadFolder);
        var installer = Path.Combine(DownloadFolder, $"locked-in-setup-{update.Version.ToString(3)}.exe");

        // HttpClient.Timeout stops applying once the headers arrive, so a stalled download needs its own limit.
        using var timeout = new CancellationTokenSource(DownloadTimeout);
        using (var client = CreateClient())
        await using (var download = await client.GetStreamAsync(update.InstallerUrl, timeout.Token))
        await using (var file = File.Create(installer))
        {
            await download.CopyToAsync(file, timeout.Token);
        }

        try
        {
            // /UPDATE=1 tells the installer to wait for this app to exit and to start the new version afterwards.
            Process.Start(new ProcessStartInfo(installer, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /UPDATE=1") { UseShellExecute = false });
        }
        catch (Win32Exception ex)
        {
            throw new IOException("The installer could not be started.", ex); // blocked by antivirus, for example
        }
    }

    /// <summary>The installer from the last update has long finished by now; don't leave it taking up space.</summary>
    private void DeleteDownloadedInstallers()
    {
        try
        {
            if (Directory.Exists(DownloadFolder))
                Directory.Delete(DownloadFolder, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(ex, "Could not delete the downloaded installer");
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LockedIn", CurrentVersion.ToString(3)));
        return client;
    }
}
