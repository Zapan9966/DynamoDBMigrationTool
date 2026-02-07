using DynamoDBMigrationLib.Migrations;
using DynamoDBMigrationLib.Migrations.Interfaces;
using System.Reflection;

namespace DynamoDBMigrationLib.Extensions;

internal static class AssemblyExtensions
{
    internal static SortedDictionary<string, IMigration> GetMigrationsDefinitions(this Assembly assembly)
    {
        var definitions = new SortedDictionary<string, IMigration>();

        var migrationDefinitions = assembly
            .GetTypes()
            .Where(t =>
                typeof(IMigration).IsAssignableFrom(t)
                && t.IsClass
                && !t.IsAbstract
            );

        foreach (var def in migrationDefinitions)
        {
            if (Attribute.GetCustomAttribute(def, typeof(MigrationAttribute)) is MigrationAttribute attribute
                && Activator.CreateInstance(def) is IMigration migration)
            { 
                definitions.Add(attribute.Id, migration);
            }
        }

        return definitions;
    }
}
