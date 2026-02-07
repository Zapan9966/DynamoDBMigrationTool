namespace DynamoDBMigrationLib.Migrations.Interfaces;

public interface IMigration
{
    void Up(MigrationBuilder migrationBuilder);

    void Down(MigrationBuilder migrationBuilder);
}
