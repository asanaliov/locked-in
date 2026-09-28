using LockedIn.Data;
using LockedIn.Data.Classification;
using LockedIn.Tracker;
using LockedIn.Tracker.Sessions;
using LockedIn.Tracker.Windows;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddLockedInSettings();

builder.Services.Configure<TrackerOptions>(builder.Configuration.GetSection(TrackerOptions.SectionName));
builder.Services.AddLockedInDatabase(builder.Configuration);
builder.Services.AddSingleton(builder.Configuration.GetSection(CategoryRules.SectionName).Get<CategoryRules>() ?? new());
builder.Services.AddSingleton<ICategoryOverrides, DbCategoryOverrides>();
builder.Services.AddSingleton<IAppClassifier, AppClassifier>();
builder.Services.AddSingleton<IActiveWindowProvider, Win32ActiveWindowProvider>();
builder.Services.AddSingleton<IIdleDetector, Win32IdleDetector>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ISessionStore, EfSessionStore>();
builder.Services.AddSingleton<SessionTracker>();
builder.Services.AddHostedService<Worker>();

builder.Build().Run();
