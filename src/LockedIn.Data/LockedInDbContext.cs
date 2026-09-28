using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LockedIn.Data;

public sealed class LockedInDbContext(DbContextOptions<LockedInDbContext> options) : DbContext(options)
{
    public DbSet<UsageSession> UsageSessions => Set<UsageSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var session = modelBuilder.Entity<UsageSession>();
        session.Property(s => s.AppName).HasMaxLength(260);
        session.Property(s => s.WindowTitle).HasMaxLength(512);
        session.Property(s => s.Category).HasConversion<string>().HasMaxLength(16);
        session.Ignore(s => s.Duration);
        session.HasIndex(s => s.StartTime);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite has no date type, so the kind is lost on the way back. Everything we store is UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => value.ToUniversalTime(),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
