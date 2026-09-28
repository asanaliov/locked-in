using LockedIn.Data;
using LockedIn.Data.Classification;
using LockedIn.Data.Metrics;
using LockedIn.Tracker;
using LockedIn.Tracker.Sessions;
using LockedIn.Tracker.Tray;
using LockedIn.Tracker.Windows;

// Two trackers would record every second twice.
using var singleInstance = new Mutex(initiallyOwned: true, @"Local\LockedIn.Tracker", out var isFirstInstance);
if (!isFirstInstance)
    return;

// Started from the Run key the working directory is System32, so pin the content root.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});
builder.Configuration.AddLockedInSettings();

builder.Services.Configure<TrackerOptions>(builder.Configuration.GetSection(TrackerOptions.SectionName));
builder.Services.Configure<MetricsOptions>(builder.Configuration.GetSection(MetricsOptions.SectionName));
builder.Services.AddLockedInDatabase(builder.Configuration);
builder.Services.AddSingleton(builder.Configuration.GetSection(CategoryRules.SectionName).Get<CategoryRules>() ?? new());
builder.Services.AddSingleton<ICategoryOverrides, DbCategoryOverrides>();
builder.Services.AddSingleton<IAppClassifier, AppClassifier>();
builder.Services.AddSingleton<IActiveWindowProvider, Win32ActiveWindowProvider>();
builder.Services.AddSingleton<IIdleDetector, Win32IdleDetector>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ISessionStore, EfSessionStore>();
builder.Services.AddSingleton<SessionTracker>();
builder.Services.AddSingleton<LiveStatusProvider>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<TrayIconService>();

builder.Build().Run();
