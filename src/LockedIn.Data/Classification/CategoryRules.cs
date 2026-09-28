namespace LockedIn.Data.Classification;

/// <summary>Default category rules from lockedin.json. Process names are matched without ".exe".</summary>
public sealed class CategoryRules
{
    public const string SectionName = "LockedIn:Categories";

    /// <summary>Apps whose window title is checked against <see cref="TitleKeywords"/>.</summary>
    public HashSet<string> Browsers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, Category> Apps { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, Category> TitleKeywords { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
