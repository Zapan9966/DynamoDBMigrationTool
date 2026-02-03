namespace DynamoDBMigrationTool.Constants;

internal static class Templates
{
    internal const string MIGRATION = @"using DynamoDBMigrator.Migrations;

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
