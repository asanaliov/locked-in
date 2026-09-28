# locked-in

[![build](https://github.com/asanaliov/locked-in/actions/workflows/build.yml/badge.svg)](https://github.com/asanaliov/locked-in/actions/workflows/build.yml)

A small Windows app that tracks your screen time and focus. It lives in the system tray, quietly
records which app you're using, sorts each one into **Focus**, **Neutral** or **Distraction**, and
gives you a daily **Locked-In Score** from 0 to 100 that says how focused you really were.

![Dashboard screenshot](docs/screenshot.png)
<!-- TODO: add a dashboard screenshot at docs/screenshot.png -->

## Features

- **One Windows app** (`LockedIn.exe`) with a tray icon and its own window, installed with a normal setup wizard
- **Background tracking** that checks the foreground window every 3 seconds and uses almost no battery
- **Idle detection**: after 2 minutes without keyboard or mouse input it stops counting
- **Categories** from simple rules: process names, plus title keywords for browsers
  (a YouTube tab is a distraction, a GitHub tab is focus)
- **Locked-In Score**, focus ratio, longest deep work streak and context switches per hour
- **Dashboard window**: today, the last 7 days, top apps, and per-app category settings
- **Tray icon** showing today's score and your current streak on hover, with an optional "Start with Windows"
- **Private by default**: everything stays in one local SQLite file, and window titles aren't saved

## How the Locked-In Score works

Each day gets three numbers, all counted over *active* (non-idle) time:

| Metric | Meaning |
| --- | --- |
| Focus ratio | Focus time ÷ active time |
| Longest streak | Longest run of Focus time. Breaks shorter than 30 s (a quick look at chat, a short idle) don't end it |
| Context switches / hour | How often you moved from one app to another |

They're combined like this:

```
score = 100 × ( 0.6 × focus ratio
              + 0.3 × min(longest streak, 90 min) / 90 min
              + 0.1 × (1 − min(switches per hour, 60) / 60) )
```

So a day spent fully in focus apps, with one 90-minute streak and no app switching, scores 100.
All the weights live in one class, [`LockedInScoreCalculator`](src/LockedIn.Data/Metrics/LockedInScoreCalculator.cs).

## Install

1. Download `locked-in-setup-x.y.z.exe` from the [latest release](https://github.com/asanaliov/locked-in/releases/latest).
2. Run it. It installs for your user only, so no admin rights are needed, and it includes everything
   it needs (no separate .NET install).
3. locked-in opens its window and starts tracking. Closing the window keeps it running in the tray;
   right-click the tray icon to reopen it, turn **Start with Windows** on or off, or exit.

The window uses the Microsoft Edge WebView2 Runtime, which is built into Windows 11 and most
up-to-date Windows 10 PCs. To uninstall, use **Settings → Apps**. Your tracking data in
`%LOCALAPPDATA%\LockedIn` is kept; delete that folder to remove it too.

## Build from source

You need Windows 10 or 11 and the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/asanaliov/locked-in.git
```

```bash
dotnet run --project src/LockedIn.App
```

```bash
dotnet test
```

To build the installer yourself, publish the app and compile [`installer/LockedIn.iss`](installer/LockedIn.iss)
with [Inno Setup 6](https://jrsoftware.org/isinfo.php):

```bash
dotnet publish src/LockedIn.App -c Release -r win-x64 --self-contained -o artifacts/publish
```

```bash
iscc /DAppVersion=0.2.0 installer/LockedIn.iss
```

Pushing a `v*` tag runs the [release workflow](.github/workflows/release.yml), which builds the
installer and attaches it to the GitHub release.

## Configuration

Settings live in `lockedin.json` next to `LockedIn.exe` (source: [`src/LockedIn.Data/lockedin.json`](src/LockedIn.Data/lockedin.json)).
Restart locked-in after editing it.
You can override any value with environment variables, for example `LockedIn__DatabasePath`.

| Setting | Default | |
| --- | --- | --- |
| `DatabasePath` | `%LOCALAPPDATA%\LockedIn\data.db` | SQLite file (WAL mode) |
| `Tracker:PollInterval` | `00:00:03` | How often the foreground window is checked |
| `Tracker:IdleThreshold` | `00:02:00` | No input for this long means idle |
| `Tracker:StoreWindowTitles` | `false` | Save window titles with each session |
| `Metrics:StreakInterruptionTolerance` | `00:00:30` | Breaks shorter than this don't end a streak |
| `Categories:Apps` | Rider, VS Code, terminals → Focus; Explorer, Settings → Neutral; Discord, Steam → Distraction | Process name (without `.exe`) → category |
| `Categories:Browsers` | Chrome, Edge, Firefox, … | Apps whose tab title is matched against the keywords |
| `Categories:TitleKeywords` | `youtube` → Distraction, `github` / `stackoverflow` / `docs` → Focus, … | Keyword in the title → category |

Apps with no rule are **Neutral**. You can change any app's category on the dashboard's **Settings**
page. That choice is stored in the database, wins over `lockedin.json`, and also updates that app's
past sessions.

## Project structure

```
LockedIn.sln
├── src/
│   ├── LockedIn.App/        LockedIn.exe: hosts the tracker and dashboard in one process,
│   │                        tray icon, WebView2 window, single instance, start with Windows
│   ├── LockedIn.Data/       EF Core DbContext, entities and migrations (SQLite),
│   │                        classification rules, metrics and the score calculator
│   ├── LockedIn.Tracker/    Background tracking: polling, idle detection, session saving
│   │   ├── Native/          NativeMethods.cs, the only place with P/Invoke (user32.dll)
│   │   ├── Windows/         IActiveWindowProvider and IIdleDetector with their Win32 versions
│   │   └── Sessions/        SessionTracker (the core logic) and the SQLite session store
│   └── LockedIn.Web/        Dashboard as a Razor class library (MVC views + Chart.js)
├── installer/               Inno Setup script
└── tests/
    └── LockedIn.Tests/      xUnit tests for session logic, classification, streaks and the score
```

The dashboard is served by ASP.NET Core inside the app on a private `127.0.0.1` port and shown in
a WebView2 window. The tracker only talks to Windows through `IActiveWindowProvider`, `IIdleDetector` and `IClock`,
so its logic is unit tested with fakes and no Windows API calls.

## Privacy

- All data stays in one SQLite file on your machine. There are no accounts, no telemetry and no
  network calls from the tracker.
- Window titles are only used to classify browser tabs and are **not saved** unless you turn on
  `StoreWindowTitles`.
- The dashboard is only reachable from your own machine (`127.0.0.1`). Its one external request is
  loading Chart.js from the jsDelivr CDN; no tracking data goes with it. Links to other sites open
  in your normal browser.
- To delete everything, remove `%LOCALAPPDATA%\LockedIn`.

## Roadmap

- [ ] Bundle Chart.js locally so the dashboard works fully offline
- [ ] Daily focus goal with a gentle tray notification
- [ ] Pick any day on the Today page
- [ ] CSV export
- [ ] Code-signed installer and a winget package
- [ ] macOS and Linux support

## Author

Made by **Asan** ([@asanaliov](https://github.com/asanaliov)).

## License

[MIT](LICENSE) © 2026 Asan
