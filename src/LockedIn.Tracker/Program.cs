using LockedIn.Tracker;
using LockedIn.Tracker.Windows;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IActiveWindowProvider, Win32ActiveWindowProvider>();
builder.Services.AddHostedService<Worker>();

builder.Build().Run();
