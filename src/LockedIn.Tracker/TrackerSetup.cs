using LockedIn.Data;
using LockedIn.Data.Classification;
using LockedIn.Tracker.Linux;
using LockedIn.Tracker.Sessions;
using LockedIn.Tracker.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LockedIn.Tracker;

public static class TrackerSetup
{
    /// <summary>Registers the background tracker and everything it needs except the database.</summary>
    public static IServiceCollection AddLockedInTracker(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TrackerOptions>(configuration.GetSection(TrackerOptions.SectionName));
        services.AddSingleton(configuration.GetSection(CategoryRules.SectionName).Get<CategoryRules>() ?? new());
        services.AddSingleton<ICategoryOverrides, DbCategoryOverrides>();
        services.AddSingleton<IAppClassifier, AppClassifier>();
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IActiveWindowProvider, Win32ActiveWindowProvider>();
            services.AddSingleton<IIdleDetector, Win32IdleDetector>();
        }
        else
        {
            services.AddSingleton<X11Connection>();
            services.AddSingleton<IActiveWindowProvider, X11ActiveWindowProvider>();
            services.AddSingleton<IIdleDetector, X11IdleDetector>();
        }
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISessionStore, EfSessionStore>();
        services.AddSingleton<SessionTracker>();
        services.AddSingleton<ICurrentSessionSource>(provider => provider.GetRequiredService<SessionTracker>());
        services.AddHostedService<Worker>();
        return services;
    }
}
