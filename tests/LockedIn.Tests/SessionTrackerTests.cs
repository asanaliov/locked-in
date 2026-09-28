using LockedIn.Data;
using LockedIn.Data.Classification;
using LockedIn.Tracker;
using LockedIn.Tracker.Sessions;
using LockedIn.Tracker.Windows;
using Microsoft.Extensions.Options;

namespace LockedIn.Tests;

public sealed class SessionTrackerTests
{
    private static readonly TimeSpan Poll = TimeSpan.FromSeconds(3);

    private readonly FakeClock _clock = new();
    private readonly FakeWindows _windows = new();
    private readonly FakeIdleDetector _idle = new();
    private readonly InMemorySessionStore _store = new();
    private readonly TrackerOptions _options = new() { IdleThreshold = TimeSpan.FromMinutes(2) };

    private SessionTracker CreateTracker()
    {
        var rules = new CategoryRules
        {
            Browsers = new(StringComparer.OrdinalIgnoreCase) { "chrome" },
            Apps = new(StringComparer.OrdinalIgnoreCase) { ["rider64"] = Category.Focus },
            TitleKeywords = new(StringComparer.OrdinalIgnoreCase) { ["youtube"] = Category.Distraction },
        };
        return new SessionTracker(_windows, _idle, new AppClassifier(rules, new FakeOverrides()), _clock, _store, Options.Create(_options));
    }

    private async Task TickAsync(SessionTracker tracker, int times = 1)
    {
        for (var i = 0; i < times; i++)
        {
            await tracker.TickAsync(CancellationToken.None);
            _clock.Advance(Poll);
        }
    }

    [Fact]
    public async Task Same_app_across_ticks_writes_nothing()
    {
        var tracker = CreateTracker();
        _windows.Current = new ActiveWindow("rider64", "Program.cs");

        await TickAsync(tracker, times: 10);

        Assert.Empty(_store.Saved);
    }

    [Fact]
    public async Task App_change_saves_the_previous_session()
    {
        var tracker = CreateTracker();
        var start = _clock.UtcNow;
        _windows.Current = new ActiveWindow("rider64", "Program.cs");
        await TickAsync(tracker, times: 4);

        _windows.Current = new ActiveWindow("Discord", "general");
        await TickAsync(tracker);

        var saved = Assert.Single(_store.Saved);
        Assert.Equal("rider64", saved.AppName);
        Assert.Equal(Category.Focus, saved.Category);
        Assert.Equal(start, saved.StartTime);
        Assert.Equal(start + 4 * Poll, saved.EndTime);
    }

    [Fact]
    public async Task Category_change_in_same_app_splits_the_session()
    {
        var tracker = CreateTracker();
        _windows.Current = new ActiveWindow("chrome", "GitHub");
        await TickAsync(tracker, times: 2);

        _windows.Current = new ActiveWindow("chrome", "YouTube");
        await TickAsync(tracker);

        var saved = Assert.Single(_store.Saved);
        Assert.Equal(Category.Neutral, saved.Category);
    }

    [Fact]
    public async Task Going_idle_closes_the_session_at_the_last_input()
    {
        var tracker = CreateTracker();
        var start = _clock.UtcNow;
        _windows.Current = new ActiveWindow("rider64", "Program.cs");
        await TickAsync(tracker, times: 100); // 5 minutes of work

        _idle.IdleTime = TimeSpan.FromMinutes(2.5);
        await TickAsync(tracker);

        var saved = Assert.Single(_store.Saved);
        Assert.Equal(start.AddMinutes(2.5), saved.EndTime);
    }

    [Fact]
    public async Task While_idle_nothing_is_tracked_and_input_starts_a_new_session()
    {
        var tracker = CreateTracker();
        _windows.Current = new ActiveWindow("rider64", "Program.cs");
        await TickAsync(tracker, times: 100);

        _idle.IdleTime = TimeSpan.FromMinutes(2.5);
        await TickAsync(tracker, times: 5);
        Assert.Single(_store.Saved);

        _idle.IdleTime = TimeSpan.Zero;
        var resumedAt = _clock.UtcNow;
        await TickAsync(tracker, times: 3);
        await tracker.FlushAsync(CancellationToken.None);

        Assert.Equal(2, _store.Saved.Count);
        Assert.Equal(resumedAt, _store.Saved[1].StartTime);
    }

    [Fact]
    public async Task Flush_saves_the_current_session()
    {
        var tracker = CreateTracker();
        _windows.Current = new ActiveWindow("rider64", "Program.cs");
        await TickAsync(tracker, times: 2);

        await tracker.FlushAsync(CancellationToken.None);

        Assert.Single(_store.Saved);
    }

    [Fact]
    public async Task Long_gap_between_ticks_ends_the_session_at_the_last_tick()
    {
        var tracker = CreateTracker();
        _windows.Current = new ActiveWindow("rider64", "Program.cs");
        await TickAsync(tracker, times: 2);
        var lastTick = _clock.UtcNow - Poll;

        _clock.Advance(TimeSpan.FromHours(1)); // laptop was asleep
        await tracker.TickAsync(CancellationToken.None);

        var saved = Assert.Single(_store.Saved);
        Assert.Equal(lastTick, saved.EndTime);
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, "Program.cs")]
    public async Task Window_title_is_only_stored_when_enabled(bool storeTitles, string? expectedTitle)
    {
        _options.StoreWindowTitles = storeTitles;
        var tracker = CreateTracker();
        _windows.Current = new ActiveWindow("rider64", "Program.cs");
        await TickAsync(tracker);

        await tracker.FlushAsync(CancellationToken.None);

        Assert.Equal(expectedTitle, Assert.Single(_store.Saved).WindowTitle);
    }
}
