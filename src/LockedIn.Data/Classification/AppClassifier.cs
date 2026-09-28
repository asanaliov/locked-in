namespace LockedIn.Data.Classification;

public interface IAppClassifier
{
    Category Classify(string appName, string windowTitle);
}

public sealed class AppClassifier(CategoryRules rules) : IAppClassifier
{
    public Category Classify(string appName, string windowTitle)
    {
        if (rules.Browsers.Contains(appName) && MatchTitleKeyword(windowTitle) is { } fromTitle)
            return fromTitle;

        return rules.Apps.GetValueOrDefault(appName, Category.Neutral);
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
