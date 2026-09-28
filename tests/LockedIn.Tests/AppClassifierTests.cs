using LockedIn.Data;
using LockedIn.Data.Classification;

namespace LockedIn.Tests;

public sealed class AppClassifierTests
{
    private readonly FakeOverrides _overrides = new();
    private readonly AppClassifier _classifier;

    public AppClassifierTests()
    {
        var rules = new CategoryRules
        {
            Browsers = new(StringComparer.OrdinalIgnoreCase) { "chrome" },
            Apps = new(StringComparer.OrdinalIgnoreCase)
            {
                ["rider64"] = Category.Focus,
                ["Discord"] = Category.Distraction,
            },
            TitleKeywords = new(StringComparer.OrdinalIgnoreCase)
            {
                ["youtube"] = Category.Distraction,
                ["github"] = Category.Focus,
            },
        };
        _classifier = new AppClassifier(rules, _overrides);
    }

    [Theory]
    [InlineData("rider64", Category.Focus)]
    [InlineData("RIDER64", Category.Focus)]
    [InlineData("Discord", Category.Distraction)]
    [InlineData("SomethingNew", Category.Neutral)]
    public void Classifies_by_process_name(string appName, Category expected) =>
        Assert.Equal(expected, _classifier.Classify(appName, "any title"));

    [Theory]
    [InlineData("Funny cats - YouTube - Google Chrome", Category.Distraction)]
    [InlineData("locked-in · GitHub - Google Chrome", Category.Focus)]
    [InlineData("New Tab - Google Chrome", Category.Neutral)]
    public void Classifies_browsers_by_title_keyword(string title, Category expected) =>
        Assert.Equal(expected, _classifier.Classify("chrome", title));

    [Fact]
    public void Title_keywords_are_ignored_for_non_browsers() =>
        Assert.Equal(Category.Distraction, _classifier.Classify("Discord", "github notifications"));

    [Fact]
    public void Override_wins_over_app_rule()
    {
        _overrides.Overrides["Discord"] = Category.Focus;

        Assert.Equal(Category.Focus, _classifier.Classify("Discord", "team chat"));
    }

    [Fact]
    public void Browser_title_keyword_wins_over_override()
    {
        _overrides.Overrides["chrome"] = Category.Focus;

        Assert.Equal(Category.Distraction, _classifier.Classify("chrome", "YouTube"));
        Assert.Equal(Category.Focus, _classifier.Classify("chrome", "New Tab"));
    }
}
