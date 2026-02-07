using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using DynamoDBMigrationLib.Extensions;
using DynamoDBMigrationLib.Extensions.AmazonDynamoDB;
using DynamoDBMigrationLib.Helpers;
using DynamoDBMigrationLib.Migrations.Interfaces;
using System.Reflection;
using System.Text;

namespace DynamoDBMigrationLib.Migrations;

internal class MigrationRunner(
    IAmazonDynamoDB client, 
    IDynamoDBContext context
) : IMigrationRunner
{
    private readonly IAmazonDynamoDB _client = client;
    private readonly IDynamoDBContext _context = context;

    #region MigrateAsync

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
        => await MigrateAsync(null, cancellationToken);

    public async Task MigrateAsync(Assembly? assembly, CancellationToken cancellationToken = default)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (assembly == null)
        {
            ConsoleHelper.WriteTitle();
        }

        await _client.CreateMigrationHistoryAsync(cancellationToken);

        assembly ??= Assembly.GetEntryAssembly()
            ?? throw new EntryPointNotFoundException("Assembly not found");

        var definitions = assembly.GetMigrationsDefinitions();
        var applied = await _client.GetAppliedMigrationAsync(cancellationToken);
        //var applied = new List<string?>();

        var migrationsToApply = definitions
            .Where(def => !applied.Any(a => a == def.Key))
            .ToDictionary();

        if (migrationsToApply.Count > 0)
        {
            foreach (var migrationToApply in migrationsToApply)
            {
                var migrationId = migrationToApply.Key;
                var migration = migrationToApply.Value;

                var builder = new MigrationBuilder();
                migration.Up(builder);

                Console.WriteLine($"\U0001F440 Applying migration {migrationId}...");

                foreach (var operation in builder)
                {
                    await operation.Execute(
                        operation.DynamoDBType == typeof(IDynamoDBContext)
                            ? _context
                            : _client,
                        cancellationToken
                    );
                }
                await _client.AddMigrationHistory(migrationId, cancellationToken);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\u2705");
                Console.ResetColor();
                Console.WriteLine($" Migration {migrationId} applied successfully.");
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"\U0001F44D Database is up to date.");
        }
    }

    #endregion

    #region MigrateDown

    public async Task MigrateDownAsync(CancellationToken cancellationToken = default)
        => await MigrateDownAsync(null, null, cancellationToken);

    public async Task MigrateDownAsync(string? migrationName, CancellationToken cancellationToken = default)
        => await MigrateDownAsync(migrationName, null, cancellationToken);

    public async Task MigrateDownAsync(string? migrationName, Assembly? assembly, CancellationToken cancellationToken = default)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (assembly == null)
        {
            ConsoleHelper.WriteTitle();
        }

        await _client.CreateMigrationHistoryAsync(cancellationToken);

        assembly ??= Assembly.GetEntryAssembly()
            ?? throw new EntryPointNotFoundException("Assembly not found");

        var applied = await _client.GetAppliedMigrationAsync(cancellationToken);

        if (applied.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\u26A0  Nothing to revert; no migration applied to the database found.");
            Console.ResetColor();
            return;
        }

        var definitions = assembly
            .GetMigrationsDefinitions()
            .Where(d => applied.Contains(d.Key))
            .OrderByDescending(d => d.Key)
            .ToDictionary();

        if (definitions.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\u26A0  The application does not contain migration.");
            Console.ResetColor();
            return;
        }

        var migrationsToRevert = new Dictionary<string, IMigration>();
        if (string.IsNullOrEmpty(migrationName))
        {
            var lastMigration = definitions.First();
            migrationsToRevert.Add(lastMigration.Key, lastMigration.Value);
        }
        else
        {
            var migrationIndex = definitions
                .ToList()
                .FindIndex(d =>
                    d.Value.GetType().Name.Equals(
                        migrationName, StringComparison.InvariantCultureIgnoreCase)
                );

            if (migrationIndex == -1)
            {
                throw new KeyNotFoundException($"There is no migration with the name {migrationName} applied to the database.");
            }

            definitions
                .Where(d => applied.Contains(d.Key))
                .OrderByDescending(d => d.Key)
                .TakeWhile((d, index) => index != migrationIndex + 1)
                .ToList()
                .ForEach(d => migrationsToRevert.Add(d.Key, d.Value));
        }

        foreach (var migrationToRevert in migrationsToRevert)
        {
            var migrationId = migrationToRevert.Key;
            var migration = migrationToRevert.Value;

            var builder = new MigrationBuilder();
            migration.Down(builder);

            Console.WriteLine($"\U0001F440 Reverting migration {migrationId}...");

            foreach (var operation in builder)
            {
                await operation.Execute(
                    operation.DynamoDBType == typeof(IDynamoDBContext)
                        ? _context
                        : _client,
                    cancellationToken
                );
            }
            await _client.DeleteMigrationHistory(migrationId, cancellationToken);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\u2705");
            Console.ResetColor();
            Console.WriteLine($" Migration {migrationId} reverted successfully.");
            Console.WriteLine();
        }
    }

    #endregion
}
