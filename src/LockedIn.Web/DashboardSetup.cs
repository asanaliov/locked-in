using LockedIn.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace LockedIn.Web;

public static class DashboardSetup
{
    public static IServiceCollection AddLockedInDashboard(this IServiceCollection services)
    {
        services.AddScoped<DashboardService>();
        services.AddScoped<CategorySettingsService>();
        services.AddScoped<GeneralSettingsService>();
        services.AddControllersWithViews().AddApplicationPart(typeof(DashboardSetup).Assembly);
        return services;
    }

    public static IEndpointRouteBuilder MapLockedInDashboard(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(name: "default", pattern: "{controller=Today}/{action=Index}");
        return endpoints;
    }
}
