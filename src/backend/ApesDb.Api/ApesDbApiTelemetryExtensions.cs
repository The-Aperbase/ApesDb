using ApesDb.Common;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace ApesDb.Api;

public static class ApesDbApiTelemetryExtensions
{
    public static void AddApesDbApiTelemetry(this WebApplicationBuilder builder)
    {
        builder.AddApesDbObservability("apesdb-api");

        var otlpHttpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_HTTP_ENDPOINT"];
        if (string.IsNullOrWhiteSpace(otlpHttpEndpoint))
        {
            otlpHttpEndpoint = "http://localhost:4318";
        }

        builder
            .Services.AddReverseProxy()
            .LoadFromMemory(
                [
                    new RouteConfig
                    {
                        RouteId = "otlp-traces",
                        ClusterId = "otlp",
                        Match = new RouteMatch { Path = "/otlp/v1/traces" },
                    }.WithTransformPathSet("/v1/traces"),
                ],
                [
                    new ClusterConfig
                    {
                        ClusterId = "otlp",
                        Destinations = new Dictionary<string, DestinationConfig>
                        {
                            ["collector"] = new() { Address = otlpHttpEndpoint },
                        },
                    },
                ]
            );
    }

    public static void MapApesDbApiTelemetry(this WebApplication app)
    {
        app.MapReverseProxy().RequireAuthorization();
    }
}
