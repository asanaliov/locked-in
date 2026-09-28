namespace LockedIn.Data.Classification;

/// <summary>A category chosen on the dashboard's Settings page. Wins over the lockedin.json defaults.</summary>
public sealed class AppCategoryOverride
{
    public required string AppName { get; set; }
    public Category Category { get; set; }
}
