using ApesDb.Api;
using ApesDb.Api.Features.Notifications;
using ApesDb.Auth;
using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Shared;
using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApesDbForwardedHeaders();
builder.Services.AddApesDbCommon();
builder.Services.AddApesDbCache(builder.Configuration);
builder.Services.AddApesDbDataProtection();
builder.Services.AddApesDbDomain(builder.Configuration);
builder.Services.AddApesDbHealthChecks();
builder.Services.AddApesDbShared();
builder.Services.AddApesDbAuth(builder.Configuration);

builder.Services.AddFastEndpoints();
builder.Services.AddSingleton<IPictureProcessor, PictureProcessor>();
builder.Services.AddNotifications();
builder.Services.AddApesDbSwagger();
builder.Services.AddSpaStaticFiles(options =>
{
    options.RootPath = "wwwroot";
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRouting();

app.Use(
    (context, next) =>
    {
        context.Request.Scheme = "https";
        return next();
    }
);

app.UseApesDbAuth();
app.UseApesDbSwagger();
app.MapHealthChecks("/health").AllowAnonymous();
app.UseFastEndpoints(config => config.Endpoints.RoutePrefix = ApiRoutes.Api.Prefix);
app.UseEndpoints(_ => { });

if (!app.Environment.IsDevelopment())
{
    app.UseSpaStaticFiles(
        new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                if (!string.Equals(context.File.Name, "sw.js", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                context.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                context.Context.Response.Headers.Pragma = "no-cache";
                context.Context.Response.Headers.Expires = "0";
            },
        }
    );
}

app.UseSpa(spa =>
{
    spa.Options.SourcePath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../../frontend/apesdb"));

    if (app.Environment.IsDevelopment())
    {
        spa.UseProxyToSpaDevelopmentServer("http://localhost:5173");
    }
});

app.Run();
