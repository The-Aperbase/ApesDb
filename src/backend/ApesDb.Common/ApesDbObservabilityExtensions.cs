using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ApesDb.Common;

public static class ApesDbObservabilityExtensions
{
    public static IHostApplicationBuilder AddApesDbObservability(
        this IHostApplicationBuilder builder,
        string serviceName
    )
    {
        var otlpSection = builder.Configuration.GetSection("OpenTelemetry:Otlp");
        var endpoint = otlpSection["Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return builder;
        }

        var resourceAttributes = builder
            .Configuration.GetSection("OpenTelemetry:ResourceAttributes")
            .GetChildren()
            .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Value))
            .ToDictionary(attribute => attribute.Key, attribute => (object)attribute.Value!);

        builder.Services.Configure<OtlpExporterOptions>(otlpSection);

        builder
            .Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName).AddAttributes(resourceAttributes))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ApesDbTelemetry.ActivitySourceName)
                    .AddSource("Npgsql")
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            if (request.Headers.TryGetValue("CF-Ray", out var cloudflareRay))
                            {
                                activity.SetTag("cloudflare.ray_id", cloudflareRay.ToString());
                            }
                        };
                    })
                    .AddHttpClientInstrumentation(options => options.RecordException = true)
                    .AddRedisInstrumentation()
                    .AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(ApesDbTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter();
            });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.AddOtlpExporter();
        });

        return builder;
    }
}
