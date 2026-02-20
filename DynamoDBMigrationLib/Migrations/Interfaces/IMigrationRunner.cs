using System.Reflection;

namespace DynamoDBMigrationLib.Migrations.Interfaces;

internal interface IInternalMigrationRunner : IMigrationRunner
{
    Task MigrateAsync(
        Assembly? assembly,
        MigrationToolOptions? options,
        CancellationToken cancellationToken = default
    );

    Task MigrateDownAsync(
        string? migrationName,
        Assembly? assembly,
        MigrationToolOptions? options,
        CancellationToken cancellationToken = default
    );
}

public interface IMigrationRunner
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
    Task MigrateDownAsync(CancellationToken cancellationToken = default);
    Task MigrateDownAsync(string? migrationName, CancellationToken cancellationToken = default);
}
