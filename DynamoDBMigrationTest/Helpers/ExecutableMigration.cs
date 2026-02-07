using Amazon.DynamoDBv2;
using DynamoDBMigrationLib.Migrations;
using DynamoDBMigrationLib.Migrations.Interfaces;

namespace DynamoDBMigrationTest.Helpers;

internal sealed class ExecutableMigration : IMigration
{
    public int ExecutionCount { get; private set; }

    public void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Query<IAmazonDynamoDB>((_, _) =>
        {
            ExecutionCount++;
            return Task.CompletedTask;
        });
    }

    public void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Query<IAmazonDynamoDB>((_, _) =>
        {
            ExecutionCount--;
            return Task.CompletedTask;
        });
    }
}
