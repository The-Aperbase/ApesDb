using System.Diagnostics;
using System.Diagnostics.Metrics;
using ApesDb.Common;

namespace ApesDb.Worker.Telemetry;

internal static class TickerQTelemetry
{
    private static readonly Counter<long> Executions = ApesDbTelemetry.Meter.CreateCounter<long>(
        "apesdb.tickerq.job.executions"
    );
    private static readonly Counter<long> Retries = ApesDbTelemetry.Meter.CreateCounter<long>(
        "apesdb.tickerq.job.retries"
    );
    private static readonly Histogram<double> Duration = ApesDbTelemetry.Meter.CreateHistogram<double>(
        "apesdb.tickerq.job.duration",
        "s"
    );

    public static async Task RunAsync(
        string functionName,
        int retryCount,
        Func<Task> action,
        Guid? runId = null,
        string? stage = null
    )
    {
        using var activity = ApesDbTelemetry.ActivitySource.StartActivity(
            $"tickerq {functionName}",
            ActivityKind.Consumer
        );
        activity?.SetTag("tickerq.function.name", functionName);
        activity?.SetTag("tickerq.retry.count", retryCount);

        if (runId.HasValue)
        {
            activity?.SetTag("apesdb.catalog.run_id", runId.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            activity?.SetTag("apesdb.catalog.stage", stage);
        }

        if (retryCount > 0)
        {
            Retries.Add(1, new KeyValuePair<string, object?>("tickerq.function.name", functionName));
        }

        var startedAt = Stopwatch.GetTimestamp();
        var outcome = "success";

        try
        {
            await action();
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception exception)
        {
            outcome = "error";
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.AddEvent(
                new ActivityEvent(
                    "exception",
                    tags: new ActivityTagsCollection
                    {
                        { "exception.type", exception.GetType().FullName },
                        { "exception.message", exception.Message },
                        { "exception.stacktrace", exception.ToString() },
                    }
                )
            );
            throw;
        }
        finally
        {
            var tags = new TagList
            {
                { "tickerq.function.name", functionName },
                { "outcome", outcome },
            };

            if (!string.IsNullOrWhiteSpace(stage))
            {
                tags.Add("apesdb.catalog.stage", stage);
            }

            Executions.Add(1, tags);
            Duration.Record(Stopwatch.GetElapsedTime(startedAt).TotalSeconds, tags);
        }
    }
}
