namespace DynamoDBMigrationLib.Migrations.Interfaces;

public interface IMigrationOperation
{
    Type DynamoDBType { get; }
    Task Execute(object dynamodb, CancellationToken cancellationToken);
}
