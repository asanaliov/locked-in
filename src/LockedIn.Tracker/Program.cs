using LockedIn.Data;
using LockedIn.Tracker;
using LockedIn.Tracker.Windows;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddLockedInSettings();

builder.Services.Configure<TrackerOptions>(builder.Configuration.GetSection(TrackerOptions.SectionName));
builder.Services.AddSingleton<IActiveWindowProvider, Win32ActiveWindowProvider>();
builder.Services.AddSingleton<IIdleDetector, Win32IdleDetector>();
builder.Services.AddHostedService<Worker>();

builder.Build().Run();
