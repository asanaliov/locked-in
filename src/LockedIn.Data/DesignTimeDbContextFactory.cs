using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LockedIn.Data;

/// <summary>Used only by `dotnet ef` to create migrations.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LockedInDbContext>
{
    public LockedInDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<LockedInDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
