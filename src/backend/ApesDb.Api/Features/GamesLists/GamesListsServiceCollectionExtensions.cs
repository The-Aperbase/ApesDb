namespace ApesDb.Api.Features.GamesLists;

public static class GamesListsServiceCollectionExtensions
{
    public static IServiceCollection AddGamesLists(this IServiceCollection services)
    {
        services.AddSingleton<IGamesListPictureProcessor, GamesListPictureProcessor>();

        return services;
    }
}
