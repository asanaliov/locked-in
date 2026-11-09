using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LockedIn.Data;
using LockedIn.Linux.Theming;
using LockedIn.Linux.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LockedIn.Linux;

public partial class App : Application
{
    internal static IHost Host { get; set; } = null!;
    internal static SingleInstance Instance { get; set; } = null!;
    internal static bool StartInTray { get; set; }

    private AppController? _controller;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = Host.Services;

        // Pages load in async void event handlers, so one failed read or save would otherwise end the app and the tracking.
        var logger = services.GetRequiredService<ILogger<App>>();
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            logger.LogError(e.Exception, "Unhandled error in the window");
            e.Handled = true;
        };

        ThemeManager.Initialize(services.GetRequiredService<IOptionsMonitor<AppearanceOptions>>());

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            _controller = new AppController(services, Instance, desktop, showWindow: !StartInTray);

        base.OnFrameworkInitializationCompleted();
    }
}
