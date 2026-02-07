using System.Reflection;

namespace DynamoDBMigrationLib.Migrations.Interfaces;

public interface IMigrationRunner
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
    Task MigrateAsync(Assembly? assembly, CancellationToken cancellationToken = default);
    Task MigrateDownAsync(CancellationToken cancellationToken = default);
    Task MigrateDownAsync(string? migrationName, CancellationToken cancellationToken = default);
    Task MigrateDownAsync(string? migrationName, Assembly? assembly, CancellationToken cancellationToken = default);
}
