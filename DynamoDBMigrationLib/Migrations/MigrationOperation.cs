using DynamoDBMigrationLib.Migrations.Interfaces;

namespace DynamoDBMigrationLib.Migrations;

internal sealed class MigrationOperation<TDynamoDB, TParam>(
    TParam parameter,
    Func<TDynamoDB, TParam, CancellationToken, Task> operation
) : IMigrationOperation
{
    public Type DynamoDBType => typeof(TDynamoDB);

    private readonly TParam _parameter = parameter;
    private readonly Func<TDynamoDB, TParam, CancellationToken, Task> _operation = operation;

    public async Task Execute(object dynamodb, CancellationToken cancellationToken)
    {
        await _operation((TDynamoDB)dynamodb, _parameter, cancellationToken);
    }
}