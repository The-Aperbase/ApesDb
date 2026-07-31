using ApesDb.Api.Features.Games;
using ApesDb.Api.Health;
using ApesDb.Api.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;

namespace ApesDb.Api;

public static class ApesDbApiServiceCollectionExtensions
{
    public static IServiceCollection AddApesDbHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", timeout: TimeSpan.FromSeconds(3))
            .AddCheck<RedisHealthCheck>("redis", timeout: TimeSpan.FromSeconds(3));

        return services;
    }

    public static IServiceCollection AddApesDbCache(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<CacheOptions>()
            .BindConfiguration(CacheOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var cacheOptions = configuration.GetRequiredSection(CacheOptions.SectionName).Get<CacheOptions>()!;
        var redisConfiguration = BuildRedisConfiguration(cacheOptions);

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfiguration));

        services.AddStackExchangeRedisCache(options =>
        {
            options.ConfigurationOptions = redisConfiguration;
        });

        services.AddFusionCacheSystemTextJsonSerializer();
        services.AddFusionCacheStackExchangeRedisBackplane(options =>
        {
            options.ConfigurationOptions = redisConfiguration;
        });

        services.AddFusionCache().TryWithAutoSetup();
        services
            .AddFusionCache(GameCache.CacheName)
            .WithCacheKeyPrefix(GameCache.CacheKeyPrefix)
            .WithDefaultEntryOptions(options => options.SetDuration(GameCache.Expiration))
            .TryWithAutoSetup();

        return services;
    }

    public static IServiceCollection AddApesDbDataProtection(this IServiceCollection services)
    {
        var connectionMultiplexer = services.BuildServiceProvider().GetRequiredService<IConnectionMultiplexer>();

        services
            .AddDataProtection()
            .PersistKeysToStackExchangeRedis(connectionMultiplexer, "ApesDb.DataProtection.Keys")
            .SetApplicationName("ApesDb");

        return services;
    }

    public static IServiceCollection AddApesDbForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    private static ConfigurationOptions BuildRedisConfiguration(CacheOptions options)
    {
        var configuration = ConfigurationOptions.Parse(options.ConnectionString);
        configuration.Password = options.Password;
        return configuration;
    }
}
