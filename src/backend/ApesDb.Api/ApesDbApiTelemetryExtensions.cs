using ApesDb.Common;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace ApesDb.Api;

public static class ApesDbApiTelemetryExtensions
{
    public static void AddApesDbApiTelemetry(this WebApplicationBuilder builder)
    {
        builder.AddApesDbObservability("apesdb-api");

        var otlpProxyEndpoint = builder.Configuration["OpenTelemetry:OtlpProxy:Endpoint"];
        if (!Uri.TryCreate(otlpProxyEndpoint, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("OpenTelemetry:OtlpProxy:Endpoint must be an absolute URI.");
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
                            ["collector"] = new() { Address = otlpProxyEndpoint },
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
