using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LockedIn.Data;

public static class DatabaseSetup
{
    private const string DefaultPath = @"%LOCALAPPDATA%\LockedIn\data.db";

    public static IServiceCollection AddLockedInDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var path = Environment.ExpandEnvironmentVariables(configuration[$"{SettingsFile.SectionName}:DatabasePath"] ?? DefaultPath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        var connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
        return services.AddDbContextFactory<LockedInDbContext>(options => options.UseSqlite(connectionString));
    }

    /// <summary>Applies migrations and switches the file to WAL so the tracker and dashboard never block each other.</summary>
    public static async Task InitializeLockedInDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var factory = services.GetRequiredService<IDbContextFactory<LockedInDbContext>>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
    }
}
