using Amazon.DynamoDBv2;
using DynamoDBMigrationLib.Helpers;
using DynamoDBMigrationLib.Migrations;
using System.Reflection;
using System.Text;

namespace DynamoDBMigrationLib.Extensions.AmazonDynamoDB;

public static class ClientExtensions
{
    #region Migrate

    public static async Task Migrate(this IAmazonDynamoDB client, CancellationToken cancellationToken = default)
        => await client.Migrate(null, cancellationToken);

    public static async Task Migrate(this IAmazonDynamoDB client, Assembly? assembly, CancellationToken cancellationToken = default)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (assembly == null)
        {
            ConsoleHelper.WriteTitle();
        }

        await client.CreateMigrationHistoryAsync(cancellationToken);

        assembly ??= Assembly.GetEntryAssembly()
            ?? throw new EntryPointNotFoundException("Assembly not found");

        var definitions = assembly.GetMigrationsDefinitions();
        var applied = await client.GetAppliedMigrationAsync(cancellationToken);

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

                foreach (var instruction in builder)
                {
                    await instruction.Execute(client, cancellationToken);
                }
                await client.AddMigrationHistory(migrationId, cancellationToken);

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

    public static async Task MigrateDown(this IAmazonDynamoDB client, CancellationToken cancellationToken = default)
        => await client.MigrateDown(null, null, cancellationToken);

    public static async Task MigrateDown(this IAmazonDynamoDB client, string? migrationName, CancellationToken cancellationToken = default)
        => await client.MigrateDown(migrationName, null, cancellationToken);

    public static async Task MigrateDown(this IAmazonDynamoDB client, string? migrationName, Assembly? assembly, CancellationToken cancellationToken = default)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (assembly == null)
        {
            ConsoleHelper.WriteTitle();
        }

        await client.CreateMigrationHistoryAsync(cancellationToken);

        assembly ??= Assembly.GetEntryAssembly()
            ?? throw new EntryPointNotFoundException("Assembly not found");

        var applied = await client.GetAppliedMigrationAsync(cancellationToken);

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

            foreach (var instruction in builder)
            {
                await instruction.Execute(client, cancellationToken);
            }
            await client.DeleteMigrationHistory(migrationId, cancellationToken);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\u2705");
            Console.ResetColor();
            Console.WriteLine($" Migration {migrationId} reverted successfully.");
            Console.WriteLine();
        }

    }

    #endregion
}
