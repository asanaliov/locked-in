using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using LockedIn.App.Tray;
using Microsoft.Extensions.Hosting;

namespace LockedIn.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var instance = SingleInstance.TryAcquire();
        if (instance is null)
        {
            SingleInstance.ActivateRunningInstance();
            return;
        }

        IHost host;
        try
        {
            host = LockedInHost.Build();
            // Task.Run keeps the host's async startup off the UI thread.
            Task.Run(() => LockedInHost.StartAsync(host)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Locked In could not start:\n\n{ex.Message}", "Locked In", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // The window is a static dashboard: software rendering skips loading the GPU driver, which saves a lot of memory.
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        var app = new App();
        app.InitializeComponent();
        var startInTray = args.Contains(StartupRegistration.StartInTrayArgument);
        using (new TrayIcon(host.Services, instance, showWindow: !startInTray))
        {
            app.Run();
        }

        // Stopping the host saves the session in progress.
        Task.Run(() => host.StopAsync()).GetAwaiter().GetResult();
        host.Dispose();
    }
}
