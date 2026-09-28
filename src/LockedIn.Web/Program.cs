using LockedIn.Data;
using LockedIn.Data.Classification;
using LockedIn.Data.Metrics;
using LockedIn.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddLockedInSettings();

builder.Services.AddLockedInDatabase(builder.Configuration);
builder.Services.Configure<MetricsOptions>(builder.Configuration.GetSection(MetricsOptions.SectionName));
builder.Services.AddSingleton(builder.Configuration.GetSection(CategoryRules.SectionName).Get<CategoryRules>() ?? new());
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CategorySettingsService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();
await app.Services.InitializeLockedInDatabaseAsync();

app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Today}/{action=Index}")
    .WithStaticAssets();

app.Run();
