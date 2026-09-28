using LockedIn.App.Tray;

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

        ApplicationConfiguration.Initialize();

        WebApplication host;
        try
        {
            host = LockedInHost.Build();
            // Task.Run keeps the host's async startup off the UI thread's synchronization context.
            Task.Run(() => LockedInHost.StartAsync(host)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"locked-in could not start:\n\n{ex.Message}", "locked-in", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var startInTray = args.Contains(StartupRegistration.StartInTrayArgument);
        using (var tray = new TrayApplicationContext(host.Services, host.Urls.First(), instance, showWindow: !startInTray))
        {
            Application.Run(tray);
        }

        // Stopping the host saves the session in progress.
        Task.Run(() => host.StopAsync()).GetAwaiter().GetResult();
    }
}
