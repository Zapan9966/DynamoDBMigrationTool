using DynamoDBMigrationLib.Abstraction;
using DynamoDBMigrationLib.Migrations;
using DynamoDBMigrationLib.Migrations.Interfaces;
using DynamoDBMigrationLib.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DynamoDBMigrationLib.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDynamoDBMigrationTool(
        this IServiceCollection services,
        DynamoDBMigrationBootstrap bootstrap
    )
    {
        bootstrap.ConfigureServices(services);

        services
            .AddSingleton<IMigrationRunner, MigrationRunner>()
            .AddSingleton<IMigrationToolConfigService, MigrationToolConfigService>();

        return services;
    }
}
