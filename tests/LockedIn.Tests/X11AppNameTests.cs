using LockedIn.Tracker.Linux;

namespace LockedIn.Tests;

public sealed class X11AppNameTests
{
    [Theory]
    [InlineData("Navigator\0firefox\0", "firefox")]
    [InlineData("code\0Code\0", "Code")]
    [InlineData("kitty\0", "kitty")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void App_name_is_the_window_class(string? windowClass, string? expected) =>
        Assert.Equal(expected, X11ActiveWindowProvider.AppNameFromClass(windowClass));
}
