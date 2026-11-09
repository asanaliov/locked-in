namespace LockedIn.Data.Classification;

/// <param name="ByTitle">True when a browser tab's title keyword decided the category, not the app's own setting.</param>
public readonly record struct Classification(Category Category, bool ByTitle);

public interface IAppClassifier
{
    Classification Classify(string appName, string windowTitle);
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
    public Classification Classify(string appName, string windowTitle)
    {
        if (rules.Browsers.Contains(appName) && MatchTitleKeyword(windowTitle) is { } fromTitle)
            return new Classification(fromTitle, ByTitle: true);

        return new Classification(overrides.Find(appName) ?? rules.DefaultFor(appName), ByTitle: false);
    }

    /// <summary>
    /// Browsers put the site's name at the end of the title, so the keyword nearest the end decides:
    /// "GitHub tutorial - YouTube" is YouTube, and "youtube-dl - GitHub" is GitHub.
    /// </summary>
    private Category? MatchTitleKeyword(string windowTitle)
    {
        Category? match = null;
        var matchEnd = -1;
        foreach (var (keyword, category) in rules.TitleKeywords)
        {
            var index = windowTitle.LastIndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index >= 0 && index + keyword.Length > matchEnd)
            {
                match = category;
                matchEnd = index + keyword.Length;
            }
        }
        return match;
    }
}
