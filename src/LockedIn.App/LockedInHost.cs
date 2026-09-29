using LockedIn.App.Services;
using LockedIn.App.Tray;
using LockedIn.App.Updates;
using LockedIn.Data;
using LockedIn.Data.Metrics;
using LockedIn.Tracker;
using LockedIn.Tracker.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LockedIn.App;

/// <summary>The in-process host: the background tracker plus the services the window reads from.</summary>
internal static class LockedInHost
{
    public static IHost Build()
    {
        // Started from the Run key the working directory is System32, so pin the content root.
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });
        builder.Configuration.AddLockedInSettings();

        // No console in a tray app; only warnings and errors go to the Windows event log.
        builder.Logging.ClearProviders();
        builder.Logging.AddEventLog();

        builder.Services.Configure<MetricsOptions>(builder.Configuration.GetSection(MetricsOptions.SectionName));
        builder.Services.Configure<AppearanceOptions>(builder.Configuration.GetSection(AppearanceOptions.SectionName));
        builder.Services.AddLockedInDatabase(builder.Configuration);
        builder.Services.AddLockedInTracker(builder.Configuration);
        builder.Services.AddSingleton<IAppIconCache, AppIconCache>();
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
}
