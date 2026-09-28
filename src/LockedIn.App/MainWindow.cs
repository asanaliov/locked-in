using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace LockedIn.App;

/// <summary>The app window: the dashboard rendered by WebView2. Links to other sites open in the browser.</summary>
internal sealed class MainWindow : Form
{
    private static readonly string WebViewDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LockedIn", "WebView2");

    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly Uri _dashboard;

    public MainWindow(string dashboardUrl)
    {
        _dashboard = new Uri(dashboardUrl);

        Text = "locked-in";
        Icon = AppIcon.Value;
        Size = new Size(1100, 850);
        MinimumSize = new Size(480, 400);
        StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(_webView);

        Load += async (_, _) => await ShowDashboardAsync();
    }

    private async Task ShowDashboardAsync()
    {
        try
        {
            // The install folder may not be writable, so keep WebView2's cache with the rest of our data.
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: WebViewDataFolder);
            await _webView.EnsureCoreWebView2Async(environment);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(
                "locked-in needs the Microsoft Edge WebView2 Runtime. The dashboard will open in your browser instead.",
                "locked-in", MessageBoxButtons.OK, MessageBoxIcon.Information);
            OpenInBrowser(_dashboard.ToString());
            Close();
            return;
        }

        var core = _webView.CoreWebView2;
        core.Settings.IsStatusBarEnabled = false;
        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            OpenInBrowser(e.Uri);
        };
        core.NavigationStarting += (_, e) =>
        {
            if (IsDashboard(e.Uri))
                return;
            e.Cancel = true;
            OpenInBrowser(e.Uri);
        };
        _webView.Source = _dashboard;
    }

    private bool IsDashboard(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Authority == _dashboard.Authority;

    private static void OpenInBrowser(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
