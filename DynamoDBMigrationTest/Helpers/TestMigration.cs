using Amazon.DynamoDBv2;
using DynamoDBMigrationLib.Migrations;
using DynamoDBMigrationLib.Migrations.Interfaces;

namespace DynamoDBMigrationTest.Helpers;

[Migration("001")]
internal sealed class TestMigration : IMigration
{
    public static int ExecutionCount;

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

[Migration("002")]
public class FirstMigration : IMigration
{
    public void Down(MigrationBuilder migrationBuilder) { }
    public void Up(MigrationBuilder migrationBuilder) { }
}

[Migration("003")]
public class SecondMigration : IMigration
{
    public void Down(MigrationBuilder migrationBuilder) { }
    public void Up(MigrationBuilder migrationBuilder) { }
}

public class MigrationWithoutAttribute : IMigration
{
    public void Down(MigrationBuilder migrationBuilder) { }
    public void Up(MigrationBuilder migrationBuilder) { }
}

[Migration("999")]
public abstract class AbstractMigration : IMigration
{
    public void Down(MigrationBuilder migrationBuilder) { }
    public void Up(MigrationBuilder migrationBuilder) { }
}

