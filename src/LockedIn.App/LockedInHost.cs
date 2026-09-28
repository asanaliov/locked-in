using LockedIn.App.Tray;
using LockedIn.Data;
using LockedIn.Data.Metrics;
using LockedIn.Tracker;
using LockedIn.Web;

namespace LockedIn.App;

/// <summary>The in-process host: background tracker plus the dashboard on a private loopback port.</summary>
internal static class LockedInHost
{
    public static WebApplication Build()
    {
        // Started from the Run key the working directory is System32, so pin the content root.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseStaticWebAssets(); // serves the dashboard's CSS under `dotnet run`; no-op once published
        builder.WebHost.UseUrls("http://127.0.0.1:0"); // loopback only, on any free port
        builder.Configuration.AddLockedInSettings();

        builder.Services.Configure<MetricsOptions>(builder.Configuration.GetSection(MetricsOptions.SectionName));
        builder.Services.Configure<AppearanceOptions>(builder.Configuration.GetSection(AppearanceOptions.SectionName));
        builder.Services.AddLockedInDatabase(builder.Configuration);
        builder.Services.AddLockedInTracker(builder.Configuration);
        builder.Services.AddLockedInDashboard();
        builder.Services.AddSingleton<LiveStatusProvider>();

        var app = builder.Build();
        app.MapStaticAssets();
        app.MapLockedInDashboard();
        return app;
    }

    public static async Task StartAsync(WebApplication app)
    {
        await app.Services.InitializeLockedInDatabaseAsync();
        await app.StartAsync();
    }
}
