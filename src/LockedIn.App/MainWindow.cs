using System.Diagnostics;
using System.Runtime.InteropServices;
using LockedIn.Data;
using Microsoft.Extensions.Options;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;

namespace LockedIn.App;

/// <summary>The app window: the dashboard rendered by WebView2. Links to other sites open in the browser.</summary>
internal sealed partial class MainWindow : Form
{
    private static readonly string WebViewDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LockedIn", "WebView2");

    // Match --bg, --text and --border in site.css so the title bar blends into the dashboard header.
    private static readonly (Color Background, Color Text, Color Border) LightTheme =
        (ColorTranslator.FromHtml("#f5f5f7"), ColorTranslator.FromHtml("#1d1d1f"), ColorTranslator.FromHtml("#e5e5ea"));
    private static readonly (Color Background, Color Text, Color Border) DarkTheme =
        (ColorTranslator.FromHtml("#000000"), ColorTranslator.FromHtml("#f5f5f7"), ColorTranslator.FromHtml("#1c1c1e"));

    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const int DwmwcpRound = 2;

    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly Uri _dashboard;
    private readonly IOptionsMonitor<AppearanceOptions> _appearance;
    private readonly IDisposable? _appearanceChanged;

    public MainWindow(string dashboardUrl, IOptionsMonitor<AppearanceOptions> appearance)
    {
        _dashboard = new Uri(dashboardUrl);
        _appearance = appearance;

        Text = "Locked In";
        Icon = AppIcon.Value;
        Size = new Size(1100, 850);
        MinimumSize = new Size(480, 400);
        StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(_webView);

        ApplyTheme();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _appearanceChanged = appearance.OnChange(_ => ApplyThemeFromAnyThread());
        Load += async (_, _) => await ShowDashboardAsync();
        Resize += async (_, _) => await PauseWhileMinimizedAsync();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            _appearanceChanged?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
            ApplyThemeFromAnyThread();
    }

    private void ApplyThemeFromAnyThread()
    {
        if (IsHandleCreated && !IsDisposed)
            BeginInvoke(ApplyTheme);
    }

    private void ApplyTheme()
    {
        var dark = IsDarkMode();
        var theme = dark ? DarkTheme : LightTheme;

        BackColor = theme.Background;
        _webView.DefaultBackgroundColor = theme.Background;

        if (!IsHandleCreated)
            return;

        // Windows 10 ignores the colour attributes and just gets a dark or light title bar.
        SetWindowAttribute(DwmwaUseImmersiveDarkMode, dark ? 1 : 0);
        SetWindowAttribute(DwmwaWindowCornerPreference, DwmwcpRound);
        SetWindowAttribute(DwmwaCaptionColor, ColorTranslator.ToWin32(theme.Background));
        SetWindowAttribute(DwmwaTextColor, ColorTranslator.ToWin32(theme.Text));
        SetWindowAttribute(DwmwaBorderColor, ColorTranslator.ToWin32(theme.Border));
    }

    private void SetWindowAttribute(int attribute, int value) =>
        DwmSetWindowAttribute(Handle, attribute, ref value, sizeof(int));

    private bool IsDarkMode() => _appearance.CurrentValue.Theme switch
    {
        Theme.Light => false,
        Theme.Dark => true,
        _ => IsWindowsDarkMode(),
    };

    private static bool IsWindowsDarkMode() =>
        Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme", 1) is 0;

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

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
                "Locked In needs the Microsoft Edge WebView2 Runtime. The dashboard will open in your browser instead.",
                "Locked In", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    /// <summary>
    /// A minimized dashboard doesn't need a live browser: suspend it so Chromium stops using CPU and trims memory.
    /// Restoring resumes it and reloads so the numbers are fresh.
    /// </summary>
    private async Task PauseWhileMinimizedAsync()
    {
        if (_webView.CoreWebView2 is not { } core)
            return;

        var minimized = WindowState == FormWindowState.Minimized;
        if (_webView.Visible != minimized)
            return; // already in the right state

        _webView.Visible = !minimized;
        if (minimized)
        {
            core.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
            await core.TrySuspendAsync();
        }
        else
        {
            core.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Normal;
            core.Reload();
        }
    }

    private bool IsDashboard(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Authority == _dashboard.Authority;

    private static void OpenInBrowser(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
