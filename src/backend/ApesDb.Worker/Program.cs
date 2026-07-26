using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Options;
using ApesDb.Igdb.Sdk;
using ApesDb.Shared;
using ApesDb.Worker;
using ApesDb.Worker.Games;
using ApesDb.Worker.Options;
using Microsoft.EntityFrameworkCore;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var databaseOptions =
    builder.Configuration.GetRequiredSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
    ?? throw new InvalidOperationException($"Missing required configuration section '{DatabaseOptions.SectionName}'.");
var dashboardOptions =
    builder.Configuration.GetRequiredSection(TickerQDashboardOptions.SectionName).Get<TickerQDashboardOptions>()
    ?? throw new InvalidOperationException(
        $"Missing required configuration section '{TickerQDashboardOptions.SectionName}'."
    );

builder.Services.AddApesDbCommon();
builder.Services.AddApesDbDomain(builder.Configuration);
builder.Services.AddApesDbShared();
builder.Services.AddIgdbSdk(builder.Configuration);
builder.Services.AddScoped<ICatalogSyncOrchestrator, CatalogSyncOrchestrator>();
builder.Services.AddScoped<ICatalogStageRunner, CatalogStageRunner>();
builder.Services.AddScoped<IPopularitySynchronizer, PopularitySynchronizer>();

builder
    .Services.AddOptions<TickerQDashboardOptions>()
    .Bind(builder.Configuration.GetRequiredSection(TickerQDashboardOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => options.BasePath.StartsWith('/'), "TickerQ dashboard base path must start with '/'.")
    .Validate(options => options.BasePath != "/", "TickerQ dashboard base path cannot be '/'.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Username), "TickerQ dashboard username is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Password), "TickerQ dashboard password is required.")
    .ValidateOnStart();

builder.Services.AddTickerQ(options =>
{
    options.AddOperationalStore(efCoreOptions =>
    {
        efCoreOptions.UseTickerQDbContext<WorkerTickerQDbContext>(
            dbContextOptions => dbContextOptions.UseNpgsql(databaseOptions.ConnectionString),
            WorkerTickerQDbContext.Schema
        );
    });

    options.AddDashboard(dashboard =>
    {
        dashboard.SetBasePath(dashboardOptions.BasePath);
        dashboard.WithBasicAuth(dashboardOptions.Username, dashboardOptions.Password);
    });
});
builder.Services.AddHostedService<InitialCatalogSyncScheduler>();

var app = builder.Build();

app.MapGet("/", () => Results.Redirect(dashboardOptions.BasePath));
app.UseTickerQ();

app.Run();
