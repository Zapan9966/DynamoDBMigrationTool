namespace DynamoDBMigrationTool.Constants;

internal static class Templates
{
    internal const string MIGRATION = @"using DynamoDBMigrationLib.Migrations;
using DynamoDBMigrationLib.Migrations.Interfaces;

namespace {{ Namespace }};

[Migration(""{{ MigrationId }}"")]
public class {{ MigrationName }} : IMigration
{
    public void Up(MigrationBuilder migrationBuilder)
    {
        throw new NotImplementedException();
    }

    public void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotImplementedException();
    }
}";
}
