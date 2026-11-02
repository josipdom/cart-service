using Cart.Application.Abstractions;
using Cart.Infrastructure.Caching;
using Cart.Infrastructure.Catalog;
using Cart.Infrastructure.Outbox;
using Cart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Cart.Infrastructure;

public static class DependencyInjection
{
    public const string ReadinessTag = "ready";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CartDbContext>(options => options.UseNpgsql(GetConnectionString(configuration, "CartDb")));
        services.AddScoped<ICartRepository, CartRepository>();

        services.AddStackExchangeRedisCache(options =>
        {
            var redis = ConfigurationOptions.Parse(GetConnectionString(configuration, "Redis"));

            // Fail fast when Redis is down, so requests fall back to the database
            // instead of waiting for the default five-second timeouts.
            redis.AbortOnConnectFail = false;
            redis.ConnectTimeout = 1000;
            redis.AsyncTimeout = 500;
            redis.SyncTimeout = 500;

            options.ConfigurationOptions = redis;
            options.InstanceName = "cart-service:";
        });
        services.AddSingleton<ICartCache, RedisCartCache>();

        services.AddSingleton<IProductCatalog, StaticProductCatalog>();

        services.AddSingleton(_ => new RabbitMqPublisher(GetConnectionString(configuration, "RabbitMq")));
        services.AddHostedService<OutboxProcessor>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddHealthChecks()
            .AddNpgSql(GetConnectionString(configuration, "CartDb"), name: "postgres", tags: [ReadinessTag])
            .AddRedis(GetConnectionString(configuration, "Redis"), name: "redis", tags: [ReadinessTag])
            .AddRabbitMQ(
                provider => provider.GetRequiredService<RabbitMqPublisher>().GetConnectionAsync(CancellationToken.None),
                name: "rabbitmq",
                tags: [ReadinessTag]);

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    private static string GetConnectionString(IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' is not configured.");
}
