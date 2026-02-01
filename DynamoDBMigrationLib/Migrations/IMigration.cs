namespace DynamoDBMigrationLib.Migrations;

public interface IMigration
{
    void Up(MigrationBuilder migrationBuilder);

    void Down(MigrationBuilder migrationBuilder);
}
