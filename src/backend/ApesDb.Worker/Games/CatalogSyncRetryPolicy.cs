namespace ApesDb.Worker.Games;

internal static class CatalogSyncRetryPolicy
{
    public const int MaxRetries = 3;

    public static int[] CreateIntervals()
    {
        return [30, 120, 600];
    }
}
