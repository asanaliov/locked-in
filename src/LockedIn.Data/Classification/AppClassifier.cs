namespace LockedIn.Data.Classification;

public interface IAppClassifier
{
    Category Classify(string appName, string windowTitle);
}

public interface ICategoryOverrides
{
    Category? Find(string appName);

    /// <summary>Call after the overrides change so the next lookup sees them.</summary>
    void Invalidate();
}

/// <summary>
/// Browser title keywords win first (a YouTube tab is a distraction whatever the browser's category),
/// then Settings page overrides, then lockedin.json app rules. Unknown apps are Neutral.
/// </summary>
public sealed class AppClassifier(CategoryRules rules, ICategoryOverrides overrides) : IAppClassifier
{
    public Category Classify(string appName, string windowTitle)
    {
        if (rules.Browsers.Contains(appName) && MatchTitleKeyword(windowTitle) is { } fromTitle)
            return fromTitle;

        return overrides.Find(appName) ?? rules.DefaultFor(appName);
    }

    private Category? MatchTitleKeyword(string windowTitle)
    {
        foreach (var (keyword, category) in rules.TitleKeywords)
        {
            if (windowTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return category;
        }
        return null;
    }
}
