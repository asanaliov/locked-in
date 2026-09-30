using LockedIn.App.Services;
using LockedIn.Data;
using LockedIn.Data.Classification;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static LockedIn.Data.Category;
using static LockedIn.Tests.Sessions;

namespace LockedIn.Tests;

public sealed class CategorySettingsServiceTests : IDisposable
{
    // An in-memory database lives as long as its connection, so every context shares this one.
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly SharedConnectionFactory _dbFactory;
    private readonly CategorySettingsService _service;

    public CategorySettingsServiceTests()
    {
        _connection.Open();
        _dbFactory = new SharedConnectionFactory(_connection);
        using (var db = _dbFactory.CreateDbContext())
            db.Database.EnsureCreated();

        var rules = new CategoryRules
        {
            Browsers = new(StringComparer.OrdinalIgnoreCase) { "chrome" },
            Apps = new(StringComparer.OrdinalIgnoreCase) { ["Discord"] = Distraction },
        };
        _service = new CategorySettingsService(_dbFactory, rules, new FakeOverrides());
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Changing_a_browser_updates_its_other_tabs_and_keeps_keyword_tabs()
    {
        Save(Session("chrome", Distraction, fromTitle: true), Session("chrome", Neutral, fromTitle: false));

        await _service.SetCategoryAsync("chrome", Focus, CancellationToken.None);

        Assert.Equal([Distraction, Focus], Categories("chrome"));
    }

    [Fact]
    public async Task Going_back_to_default_restores_the_default_category()
    {
        Save(Session("chrome", Neutral, fromTitle: false));

        await _service.SetCategoryAsync("chrome", Focus, CancellationToken.None);
        await _service.SetCategoryAsync("chrome", null, CancellationToken.None);

        Assert.Equal([Neutral], Categories("chrome"));
    }

    [Fact]
    public async Task Changing_a_regular_app_updates_all_its_sessions()
    {
        Save(Session("Discord", Distraction, fromTitle: false), Session("Discord", Distraction, fromTitle: false));

        await _service.SetCategoryAsync("Discord", Focus, CancellationToken.None);

        Assert.Equal([Focus, Focus], Categories("Discord"));
    }

    private static UsageSession Session(string app, Category category, bool fromTitle)
    {
        var session = At(0, 10, category, app);
        session.CategoryFromTitle = fromTitle;
        return session;
    }

    private void Save(params UsageSession[] sessions)
    {
        using var db = _dbFactory.CreateDbContext();
        db.UsageSessions.AddRange(sessions);
        db.SaveChanges();
    }

    private Category[] Categories(string app)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.UsageSessions.Where(s => s.AppName == app).OrderBy(s => s.Id).Select(s => s.Category).ToArray();
    }

    private sealed class SharedConnectionFactory(SqliteConnection connection) : IDbContextFactory<LockedInDbContext>
    {
        public LockedInDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<LockedInDbContext>().UseSqlite(connection).Options);
    }
}
