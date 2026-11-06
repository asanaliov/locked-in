using LockedIn.Data;
using LockedIn.Data.Classification;
using Microsoft.Extensions.Configuration;

namespace LockedIn.Tests;

/// <summary>Loads the lockedin.json that ships with the app, because a mistake in it stops the app from starting.</summary>
public sealed class DefaultSettingsTests
{
    private static readonly CategoryRules Rules = new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "lockedin.json"))
        .Build()
        .GetSection(CategoryRules.SectionName)
        .Get<CategoryRules>()!;

    private readonly AppClassifier _classifier = new(Rules, new FakeOverrides());

    [Theory]
    [InlineData("Code", Category.Focus)]
    [InlineData("rider64", Category.Focus)]
    [InlineData("jetbrains-rider", Category.Focus)]
    [InlineData("kitty", Category.Focus)]
    [InlineData("org.gnome.Nautilus", Category.Neutral)]
    [InlineData("discord", Category.Distraction)]
    public void Common_apps_have_a_category(string appName, Category expected) =>
        Assert.Equal(expected, _classifier.Classify(appName, "any title").Category);

    [Theory]
    [InlineData("chrome", "How to read the docs - YouTube - Google Chrome", Category.Distraction)]
    [InlineData("msedge", "r/github - Reddit - Microsoft Edge", Category.Distraction)]
    [InlineData("firefox", "GitHub tutorial - YouTube — Mozilla Firefox", Category.Distraction)]
    [InlineData("Google-chrome", "torvalds/linux - GitHub - Google Chrome", Category.Focus)]
    [InlineData("Chromium", "YouTube API docs", Category.Focus)]
    public void Browser_tabs_are_sorted_by_the_site_they_show(string browser, string title, Category expected) =>
        Assert.Equal(expected, _classifier.Classify(browser, title).Category);
}
