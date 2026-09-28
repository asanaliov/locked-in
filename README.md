# locked-in

[![build](https://github.com/asanaliov/locked-in/actions/workflows/build.yml/badge.svg)](https://github.com/asanaliov/locked-in/actions/workflows/build.yml)

A small Windows screen time and focus tracker. It quietly records which app you're using, sorts
each one into **Focus**, **Neutral** or **Distraction**, and gives you a daily **Locked-In Score**
from 0 to 100 that says how focused you really were.

![Dashboard screenshot](docs/screenshot.png)
<!-- TODO: add a dashboard screenshot at docs/screenshot.png -->

## Features

- **Background tracker** that checks the foreground window every 3 seconds and uses almost no battery
- **Idle detection**: after 2 minutes without keyboard or mouse input it stops counting
- **Categories** from simple rules: process names, plus title keywords for browsers
  (a YouTube tab is a distraction, a GitHub tab is focus)
- **Locked-In Score**, focus ratio, longest deep work streak and context switches per hour
- **Dashboard** on `localhost`: today, the last 7 days, top apps, and per-app category settings
- **Tray icon** showing today's score and your current streak, with an optional "Start with Windows"
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

## Getting started

You need Windows 10 or 11 and the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/asanaliov/locked-in.git
cd locked-in
dotnet build
```

Start the tracker. It has no window; look for the icon in the system tray.

```bash
dotnet run --project src/LockedIn.Tracker
```

Start the dashboard, then open <http://localhost:5080>.

```bash
dotnet run --project src/LockedIn.Web
```

Run the tests:

```bash
dotnet test
```

Right-click the tray icon to open the dashboard, turn **Start with Windows** on or off, or exit.
Each session is saved when you switch apps or go idle, so the one you're in right now shows up
on the dashboard a little later. The tray tooltip already counts it.

## Configuration

Shared settings live in [`src/LockedIn.Data/lockedin.json`](src/LockedIn.Data/lockedin.json) and are copied next to both apps.
You can override any value with environment variables, for example `LockedIn__DatabasePath`.

| Setting | Default | |
| --- | --- | --- |
| `DatabasePath` | `%LOCALAPPDATA%\LockedIn\data.db` | SQLite file shared by the tracker and the dashboard (WAL mode) |
| `Tracker:PollInterval` | `00:00:03` | How often the foreground window is checked |
| `Tracker:IdleThreshold` | `00:02:00` | No input for this long means idle |
| `Tracker:StoreWindowTitles` | `false` | Save window titles with each session |
| `Tracker:DashboardUrl` | `http://localhost:5080` | Opened from the tray icon |
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
│   ├── LockedIn.Data/       EF Core DbContext, entities and migrations (SQLite),
│   │                        classification rules, metrics and the score calculator
│   ├── LockedIn.Tracker/    Worker service: polling, idle detection, session saving, tray icon
│   │   ├── Native/          NativeMethods.cs, the only place with P/Invoke (user32.dll)
│   │   ├── Windows/         IActiveWindowProvider and IIdleDetector with their Win32 versions
│   │   ├── Sessions/        SessionTracker (the core logic) and the SQLite session store
│   │   └── Tray/            Tray icon, live status, start-with-Windows registration
│   └── LockedIn.Web/        ASP.NET Core MVC dashboard (Razor views + Chart.js)
└── tests/
    └── LockedIn.Tests/      xUnit tests for session logic, classification, streaks and the score
```

The tracker only talks to Windows through `IActiveWindowProvider`, `IIdleDetector` and `IClock`,
so its logic is unit tested with fakes and no Windows API calls.

## Privacy

- All data stays in one SQLite file on your machine. There are no accounts, no telemetry and no
  network calls from the tracker.
- Window titles are only used to classify browser tabs and are **not saved** unless you turn on
  `StoreWindowTitles`.
- The dashboard listens on `localhost` only. Its one external request is loading Chart.js from the
  jsDelivr CDN; no tracking data goes with it.
- To delete everything, remove `%LOCALAPPDATA%\LockedIn`.

## Roadmap

- [ ] Serve the dashboard from the tracker so there's only one thing to run
- [ ] Bundle Chart.js locally so the dashboard works fully offline
- [ ] Daily focus goal with a gentle tray notification
- [ ] Pick any day on the Today page
- [ ] CSV export
- [ ] Installer (MSIX or winget)
- [ ] macOS and Linux support

## License

[MIT](LICENSE)
