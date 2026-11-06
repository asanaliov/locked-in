using LockedIn.Core.Services;
using LockedIn.Core.Updates;
using LockedIn.Data;
using LockedIn.Data.Metrics;
using LockedIn.Tracker;
using LockedIn.Tracker.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LockedIn.Linux;

/// <summary>The in-process host: the background tracker plus the services the window reads from.</summary>
internal static class LockedInHost
{
    public static IHost Build()
    {
        // Started from a menu or at login the working directory could be anywhere, so pin the content root.
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });
        builder.Configuration.AddLockedInSettings();

        // Warnings and errors go to stderr, which ends up in the journal when started from a desktop session.
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // By default a background service that throws stops the host, which would end tracking while the tray icon stays up.
        builder.Services.Configure<HostOptions>(options => options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

        builder.Services.Configure<MetricsOptions>(builder.Configuration.GetSection(MetricsOptions.SectionName));
        builder.Services.Configure<AppearanceOptions>(builder.Configuration.GetSection(AppearanceOptions.SectionName));
        builder.Services.AddLockedInDatabase(builder.Configuration);
        builder.Services.AddLockedInTracker(builder.Configuration);
        builder.Services.AddSingleton<IAppIconCache, NoAppIcons>();
        builder.Services.AddSingleton<LiveStatusProvider>();
        builder.Services.AddSingleton<DashboardService>();
        builder.Services.AddSingleton<CategorySettingsService>();
        builder.Services.AddSingleton<GeneralSettingsService>();
        builder.Services.Configure<UpdateOptions>(builder.Configuration.GetSection(UpdateOptions.SectionName));
        builder.Services.AddSingleton<UpdateService>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<UpdateService>());
        return builder.Build();
    }

    public static async Task StartAsync(IHost host)
    {
        await host.Services.InitializeLockedInDatabaseAsync();
        await host.StartAsync();
    }

    /// <summary>The X11 tracker reports no executable path, so the pages show each app's initial.</summary>
    private sealed class NoAppIcons : IAppIconCache
    {
        public void Remember(string appName, string executablePath) { }
    }
}
