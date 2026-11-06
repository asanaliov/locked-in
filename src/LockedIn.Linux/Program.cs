using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Hosting;

namespace LockedIn.Linux;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        using var instance = SingleInstance.TryAcquire();
        if (instance is null)
        {
            SingleInstance.ActivateRunningInstance();
            return 0;
        }

        IHost host;
        try
        {
            host = LockedInHost.Build();
            Task.Run(() => LockedInHost.StartAsync(host)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Locked In could not start: {ex.GetBaseException().Message}");
            return 1;
        }

        App.Host = host;
        App.Instance = instance;
        App.StartInTray = args.Contains(Autostart.StartInTrayArgument);

        // Logging out or `kill` sends these; without handling them the session in progress would be lost.
        using var terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, Shutdown);
        using var interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, Shutdown);
        using var hangUp = PosixSignalRegistration.Create(PosixSignal.SIGHUP, Shutdown);

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Locked In could not open its window: {ex.Message}");
            return 1;
        }
        finally
        {
            // Stopping the host saves the session in progress.
            Task.Run(() => host.StopAsync()).GetAwaiter().GetResult();
            host.Dispose();
        }
    }

    private static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        // The window is a static dashboard, and software rendering also works where OpenGL doesn't, as in a VM.
        .With(new X11PlatformOptions { RenderingMode = [X11RenderingMode.Software], WmClass = "locked-in" });

    private static void Shutdown(PosixSignalContext context)
    {
        context.Cancel = true;
        Dispatcher.UIThread.Post(() =>
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown());
    }
}
